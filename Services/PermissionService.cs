using BackendApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BackendApp.Services;

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsForRolesAsync(IEnumerable<string> roles);
    Task InvalidateRolePermissionsCacheAsync(int roleId);
}

public class PermissionService : IPermissionService
{
    // Token dùng chung để xóa toàn bộ cache phân quyền cùng lúc (phù hợp khi chạy 1 instance)
    private static CancellationTokenSource _resetSource = new();

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public PermissionService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<HashSet<string>> GetPermissionsForRolesAsync(IEnumerable<string> roles)
    {
        var roleList = roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(r => r.ToUpperInvariant())
            .OrderBy(r => r)
            .ToList();

        var cacheKey = $"role_permissions_{string.Join("_", roleList)}";

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            entry.AddExpirationToken(new CancellationChangeToken(_resetSource.Token));

            var codes = await _dbContext.RolePermissions
                .Where(rp => roleList.Contains(rp.Role.Name.ToUpper()) && rp.Permission.IsActive)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync();

            return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        });

        return result ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public Task InvalidateRolePermissionsCacheAsync(int roleId)
    {
        // Hủy token cũ -> toàn bộ cache phân quyền hết hiệu lực ngay lập tức
        var old = Interlocked.Exchange(ref _resetSource, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
        return Task.CompletedTask;
    }
}