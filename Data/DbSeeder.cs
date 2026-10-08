using BackendApp.Constants;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(AppDbContext context)
    {
        // 1. Seed các Role mặc định (bổ sung role còn thiếu)
        var defaultRoles = new[]
        {
            (UserRoles.Admin, "Quản trị viên hệ thống"),
            (UserRoles.Manager, "Quản lý nhân sự"),
            (UserRoles.Employee, "Nhân viên")
        };

        foreach (var (name, description) in defaultRoles)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == name))
            {
                context.Roles.Add(new Role
                {
                    Name = name,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed tài khoản Admin mặc định
        if (!await context.Employees.AnyAsync(e => e.Username == "admin"))
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Admin);

            if (adminRole != null)
            {
                var adminEmployee = new Employee
                {
                    EmployeeCode = "EMP-001",
                    Username = "admin",
                    Email = "admin@company.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "System Administrator",
                    Status = "ACTIVE",
                    HireDate = DateTime.UtcNow,
                    RoleId = adminRole.Id
                };

                await context.Employees.AddAsync(adminEmployee);
                await context.SaveChangesAsync();
            }
        }
    }
}