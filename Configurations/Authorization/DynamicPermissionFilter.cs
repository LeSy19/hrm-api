// namespace BackendApp.Configurations.Authorization;

// using System.Security.Claims;
// using BackendApp.constants;
// using BackendApp.Services;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Controllers;
// using Microsoft.AspNetCore.Mvc.Filters;

// public class DynamicPermissionFilter : IAsyncActionFilter
// {
//     private readonly IPermissionService _permissionService;

//     public DynamicPermissionFilter(IPermissionService permissionService)
//     {
//         _permissionService = permissionService;
//     }

//     public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//     {
//         // 1. Cho phép qua nếu Endpoint có [AllowAnonymous]
//         var endpoint = context.HttpContext.GetEndpoint();
//         if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
//         {
//             await next();
//             return;
//         }

//         // 2. Lấy HTTP Method và Route Pattern hiện tại
//         var httpMethod = context.HttpContext.Request.Method;
//         var descriptor = context.ActionDescriptor as ControllerActionDescriptor;

//         if (descriptor == null)
//         {
//             await next();
//             return;
//         }

//         // Tự động dựng Route Template chuẩn hóa (VD: "api/employee/{id}")
//         var routeTemplate = descriptor.AttributeRouteInfo?.Template ?? $"api/{descriptor.ControllerName}";

//         // 3. Tra cứu trong PermissionCatalog xem Endpoint này có yêu cầu Permission nào không
//         var requiredPermission = PermissionCatalog.GetRequiredPermission(httpMethod, routeTemplate);

//         // Nếu API này không yêu cầu Permission nào trong Catalog -> Cho qua
//         if (string.IsNullOrEmpty(requiredPermission))
//         {
//             await next();
//             return;
//         }

//         // 4. Kiểm tra User đã Authenticate (JWT) chưa
//         var user = context.HttpContext.User;
//         if (user?.Identity?.IsAuthenticated != true)
//         {
//             context.Result = new UnauthorizedResult();
//             return;
//         }

//         // 5. Kiểm tra Roles từ Claims
//         var roles = user.Claims
//             .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
//             .Select(c => c.Value)
//             .ToList();

//         // Admin có toàn quyền (Bypass check)
//         if (roles.Contains("ADMIN"))
//         {
//             await next();
//             return;
//         }

//         // 6. Lấy danh sách Permission Codes của User từ Cache/DB
//         var userPermissions = await _permissionService.GetPermissionsForRolesAsync(roles);

//         if (userPermissions.Contains(requiredPermission))
//         {
//             await next(); // Đúng quyền -> Cho phép chạy tiếp vào Controller Action
//         }
//         else
//         {
//             // Không có quyền -> Trả về HTTP 403 Forbidden
//             context.Result = new ObjectResult(new
//             {
//                 message = $"Bạn không có quyền [{requiredPermission}] để thực hiện thao tác này."
//             })
//             {
//                 StatusCode = StatusCodes.Status403Forbidden
//             };
//         }
//     }
// }