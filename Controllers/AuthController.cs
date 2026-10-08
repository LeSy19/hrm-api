using System.Security.Claims;
using BackendApp.Configurations.Authorization;
using BackendApp.Constants;
using BackendApp.Data;
using BackendApp.DTOs.Auth;
using BackendApp.Models;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var (response, refreshToken) = await _authService.LoginAsync(dto);
        if (response == null)
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." });

        SetRefreshTokenCookie(refreshToken!);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Không tìm thấy Refresh Token." });

        var (result, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
        if (result == null || newRefreshToken == null)
            return Unauthorized(new { message = "Refresh Token không hợp lệ hoặc đã hết hạn." });

        SetRefreshTokenCookie(newRefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [AuthenticatedOnly]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
        }

        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Đăng xuất thành công." });
    }

    /// <summary>
    /// Quyền của user hiện tại, để frontend ẩn/hiện menu và nút.
    /// Mỗi phần tử gồm code, httpMethod, routeTemplate.
    /// ADMIN nhận toàn bộ permission đang hoạt động.
    /// </summary>
    [HttpGet("me/permissions")]
    [Authorize]
    [AuthenticatedOnly]
    public async Task<IActionResult> MyPermissions([FromServices] AppDbContext db)
    {
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value.ToUpper())
            .ToList();

        var isAdmin = roles.Contains(UserRoles.Admin.ToUpper());

        IQueryable<Permission> query = isAdmin
            ? db.Permissions.Where(p => p.IsActive)
            : db.RolePermissions
                .Where(rp => roles.Contains(rp.Role.Name.ToUpper()) && rp.Permission.IsActive)
                .Select(rp => rp.Permission)
                .Distinct();

        var items = await query
            .Select(p => new { p.Code, p.HttpMethod, p.RouteTemplate })
            .ToListAsync();

        return Ok(new { isAdmin, permissions = items });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}