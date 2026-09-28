using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Quản trị viên hệ thống" },
                new Role { Name = "Manager", Description = "Quản lý nhân sự" },
                new Role { Name = "Employee", Description = "Nhân viên" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // 2. Seed tài khoản Admin mặc định nếu chưa có Employee nào
        if (!await context.Employees.AnyAsync(e => e.Username == "admin"))
        {
            // Lấy Role Admin chuẩn từ Database
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");

            if (adminRole != null)
            {
                var adminEmployee = new Employee
                {
                    EmployeeCode = "EMP-001",
                    Username = "admin",
                    Email = "admin@company.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Mật khẩu: Admin@123
                    FullName = "System Administrator",
                    Status = "ACTIVE",
                    HireDate = DateTime.UtcNow,
                    RoleId = adminRole.Id // Gán RoleId tự động theo ID của Role Admin
                };

                await context.Employees.AddAsync(adminEmployee);
                await context.SaveChangesAsync();
            }
        }
    }


}