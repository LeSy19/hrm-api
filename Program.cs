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
using BackendApp.Configurations;
using BackendApp.Configurations.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;

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

// Phân quyền động theo endpoint
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();

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

// Controllers + filter phân quyền toàn cục theo endpoint
builder.Services.AddControllers(options =>
{
    options.Filters.Add<EndpointPermissionFilter>();
});
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

// --- KIỂM TRA KẾT NỐI DB, MIGRATE & SEED DATA (1 SCOPE DUY NHẤT) ---
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

        logger.LogInformation("Đang thực thi Migrations vào MySQL...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrate Database hoàn tất thành công!");

        // Seed Roles mặc định + tài khoản admin
        await DbSeeder.SeedRolesAsync(context);
        logger.LogInformation("Seed Roles/Admin thành công!");

        // Quét toàn bộ endpoint trong controller và đồng bộ vào bảng Permissions
        await EndpointPermissionSync.SyncAsync(context,
            services.GetRequiredService<IActionDescriptorCollectionProvider>());
        logger.LogInformation("Đồng bộ Endpoint -> Permissions thành công!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, ">>> [DATABASE] Lỗi kết nối, Migrate hoặc Seed thất bại! <<<");
        if (app.Environment.IsDevelopment()) throw; // Dev: dừng app để thấy lỗi ngay
    }
}

// 5. Cấu hình Scalar UI trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard("/hangfire");

app.Run();