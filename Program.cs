using BackendApp.Data;
using BackendApp.Services;
using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using Scalar.AspNetCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using BackendApp.Configurations;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Thiếu connection string 'DefaultConnection'.");

// 1. Cấu hình DbContext MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Cấu hình Hangfire với MySQL (Database riêng)
var hangfireConn = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? throw new InvalidOperationException("Thiếu connection string 'HangfireConnection'.");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(hangfireConn, new MySqlStorageOptions
    {
        TransactionIsolationLevel = IsolationLevel.ReadCommitted,
        QueuePollInterval = TimeSpan.FromSeconds(15),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        PrepareSchemaIfNecessary = true,
        DashboardJobListLimit = 50000,
        TransactionTimeout = TimeSpan.FromMinutes(1),
        TablesPrefix = "hangfire_"
    })));
builder.Services.AddHangfireServer();

// 3. Đăng ký Services & Hangfire Job
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<JobTitleService>();
builder.Services.AddScoped<LeaveTypeService>();
builder.Services.AddScoped<LeaveBalanceService>();
builder.Services.AddScoped<LeaveRequestService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddControllers();


// Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Custom Trả về lỗi 401 và 403 dạng JSON chuẩn
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { statusCode = 401, message = "Bạn chưa đăng nhập hoặc Token đã hết hạn." });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { statusCode = 403, message = "Bạn không có quyền truy cập chức năng này." });
        }
    };
});



// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "CoreHR API Reference";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});
builder.Services.AddAppAuthorizationPolicies();

var app = builder.Build();

// --- TỐI ƯU GỘP: KIỂM TRA KẾT NỐI DB, MIGRATE & SEED DATA (DÙNG 1 SCOPE DUY NHẤT) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (await context.Database.CanConnectAsync())
        {
            logger.LogInformation(">>> [DATABASE] Kết nối MySQL Database THÀNH CÔNG! <<<");
        }
        else
        {
            logger.LogWarning(">>> [DATABASE] Không thể kết nối tới MySQL Database! <<<");
        }

        // Tự động Migrate toàn bộ Schema mới nhất bất đồng bộ
        logger.LogInformation("Đang thực thi Migrations vào MySQL...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrate Database hoàn tất thành công!");

        // Gọi Seeder tự động nạp Roles
        await DbSeeder.SeedRolesAsync(context);
        logger.LogInformation("Seed Data Roles thành công!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, ">>> [DATABASE] Lỗi kết nối hoặc thực thi Migrate Database thất bại! <<<");
    }
}

// 5. Cấu hình Scalar UI trong môi trường Development
if (app.Environment.IsDevelopment())
{
    // BẮT BUỘC: Thêm .AllowAnonymous() cho CẢ HAI endpoint này
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseCors("AllowFrontend");
//Thêm 2 Middleware này theo đúng thứ tự (Trước MapControllers)
app.UseAuthentication(); // 1. Xác định User là ai từ JWT Token
app.UseAuthorization();  // 2. Kiểm tra User có quyền gì

app.MapControllers();
app.UseHangfireDashboard("/hangfire");

app.Run();