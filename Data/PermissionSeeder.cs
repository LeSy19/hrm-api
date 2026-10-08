// using BackendApp.constants;
// using BackendApp.Models;
// using Microsoft.EntityFrameworkCore;

// namespace BackendApp.Data;

// public static class PermissionSeeder
// {
//     public static async Task SeedPermissionsAsync(AppDbContext context)
//     {
//         var existingPermissions = await context.Permissions.ToDictionaryAsync(p => p.Code);
//         var catalogPermissions = PermissionCatalog.AllPermissions;

//         var permissionsToInsert = new List<Permission>();

//         foreach (var def in catalogPermissions)
//         {
//             if (!existingPermissions.TryGetValue(def.Code, out var existing))
//             {
//                 permissionsToInsert.Add(new Permission
//                 {
//                     Code = def.Code,
//                     Name = def.Name,
//                     Description = def.Description,
//                     Module = def.Module,
//                     Action = def.Action,
//                     DisplayOrder = def.DisplayOrder,
//                     IsActive = true,
//                     CreatedAt = DateTime.UtcNow
//                 });
//             }
//             else
//             {
//                 // Cập nhật thông tin mô tả/tên hiển thị nếu catalog có chỉnh sửa
//                 existing.Name = def.Name;
//                 existing.Description = def.Description;
//                 existing.Module = def.Module;
//                 existing.Action = def.Action;
//                 existing.DisplayOrder = def.DisplayOrder;
//             }
//         }

//         if (permissionsToInsert.Any())
//         {
//             await context.Permissions.AddRangeAsync(permissionsToInsert);
//             await context.SaveChangesAsync();
//         }
//         // 2. Seed Các Roles mặc định nếu chưa tồn tại
//         var defaultRoles = new[] { "ADMIN", "MANAGER", "EMPLOYEE" };
//         foreach (var roleName in defaultRoles)
//         {
//             if (!await context.Roles.AnyAsync(r => r.Name == roleName))
//             {
//                 await context.Roles.AddAsync(new Role
//                 {
//                     Name = roleName,
//                     Description = $"Role {roleName} mặc định hệ thống",
//                     IsActive = true,
//                     CreatedAt = DateTime.UtcNow
//                 });
//             }
//         }
//         await context.SaveChangesAsync();

//         // 3. Gán TOÀN BỘ Permissions cho Role ADMIN
//         var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "ADMIN");
//         if (adminRole != null)
//         {
//             var allDbPermissions = await context.Permissions.ToListAsync();
//             var existingAdminPermissionIds = await context.RolePermissions
//                 .Where(rp => rp.RoleId == adminRole.Id)
//                 .Select(rp => rp.PermissionId)
//                 .ToListAsync();

//             var newRolePermissionsForAdmin = allDbPermissions
//                 .Where(p => !existingAdminPermissionIds.Contains(p.Id))
//                 .Select(p => new RolePermission
//                 {
//                     RoleId = adminRole.Id,
//                     PermissionId = p.Id,
//                     AssignedAt = DateTime.UtcNow
//                 })
//                 .ToList();

//             if (newRolePermissionsForAdmin.Any())
//             {
//                 await context.RolePermissions.AddRangeAsync(newRolePermissionsForAdmin);
//                 await context.SaveChangesAsync();
//             }
//         }


//     }
// }