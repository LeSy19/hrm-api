// using System.Security.Claims;
// using BackendApp.Services;
// using Microsoft.AspNetCore.Authorization;

// namespace BackendApp.Configurations.Authorization;

// public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
// {
//     private readonly IPermissionService _permissionService;

//     public PermissionAuthorizationHandler(IPermissionService permissionService)
//     {
//         _permissionService = permissionService;
//     }

//     protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
//     {
//         if (context.User?.Identity?.IsAuthenticated != true)
//         {
//             return;
//         }

//         // Lấy danh sách Roles từ Claims trong JWT Token
//         var roles = context.User.Claims
//             .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
//             .Select(c => c.Value)
//             .ToList();

//         if (!roles.Any()) return;

//         // ADMIN bypass tất cả các bước kiểm tra (nếu cần)
//         if (roles.Contains("ADMIN"))
//         {
//             context.Succeed(requirement);
//             return;
//         }

//         // Lấy danh sách Permission Codes từ Cache / DB
//         var userPermissions = await _permissionService.GetPermissionsForRolesAsync(roles);

//         if (userPermissions.Contains(requirement.Permission))
//         {
//             context.Succeed(requirement);
//         }
//     }
// }