using BackendApp.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BackendApp.Configurations;

public static class RolePolicySetup
{
    // Danh sách Tên Policy dùng trong thuộc tính [Authorize(Policy = ...)]
    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";         // Chỉ Admin
        public const string Management = "Management";   // Admin và Manager
        public const string StaffAccess = "StaffAccess";     // Cả 3 Roles (Admin, Manager, Employee)
    }

    public static void AddAppAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // 1. Policy CHỈ ADMIN (Ví dụ: Xóa nhân viên, Xóa phòng ban)
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireRole(UserRoles.Admin));

            // 2. Policy QUẢN LÝ NHÂN SỰ (Admin + Manager) (Ví dụ: Tạo/Sửa nhân viên, Tạo/Sửa phòng ban)
            options.AddPolicy(Policies.Management, policy =>
                policy.RequireRole(UserRoles.Admin, UserRoles.Manager));

            // 3. Policy TOÀN BỘ NHÂN VIÊN (Admin + Manager + Employee) (Ví dụ: Xem danh sách phòng ban, Tạo đơn xin nghỉ)
            options.AddPolicy(Policies.StaffAccess, policy =>
                policy.RequireRole(UserRoles.Admin, UserRoles.Manager, UserRoles.Employee));

            // KHÓA MẶC ĐỊNH: Mọi API không khai báo gì thêm đều bắt buộc phải đăng nhập (Authenticated)
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}