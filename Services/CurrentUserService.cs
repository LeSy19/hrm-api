using System.Security.Claims;

namespace BackendApp.Services;

public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    List<string> Roles { get; }
    Task<bool> HasPermissionAsync(string permissionCode);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IPermissionService permissionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    public int UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var userId) ? userId : 0;
        }
    }

    public string Username => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;

    public List<string> Roles => _httpContextAccessor.HttpContext?.User?.Claims
        .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
        .Select(c => c.Value)
        .ToList() ?? new List<string>();

    public async Task<bool> HasPermissionAsync(string permissionCode)
    {
        if (!Roles.Any()) return false;
        var permissions = await _permissionService.GetPermissionsForRolesAsync(Roles);
        return permissions.Contains(permissionCode);
    }
}