using System.Security.Claims;
using BackendApp.Constants;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BackendApp.Configurations.Authorization;

/// <summary>
/// Filter toàn cục: kiểm tra role của user có được cấp endpoint đang gọi hay không.
/// ADMIN luôn được qua. Endpoint chưa được cấp cho role thì bị từ chối (403).
/// </summary>
public class EndpointPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionService _permissionService;

    public EndpointPermissionFilter(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor d) return;

        // Endpoint công khai hoặc chỉ cần đăng nhập thì bỏ qua
        if (d.EndpointMetadata.Any(m => m is IAllowAnonymous || m is AuthenticatedOnlyAttribute)) return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        if (roles.Contains(UserRoles.Admin, StringComparer.OrdinalIgnoreCase)) return;

        var code = PermissionKey.From(d);
        var allowed = await _permissionService.GetPermissionsForRolesAsync(roles);

        if (!allowed.Contains(code))
        {
            context.Result = new ObjectResult(new
            {
                statusCode = 403,
                message = $"Bạn không có quyền [{code}] để thực hiện thao tác này."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}