using BackendApp.DTOs.Auth;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Mở khóa toàn bộ API trong Controller này
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var (response, refreshToken) = await _authService.LoginAsync(dto);
        if (response == null)
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không chính xác." });

        // Lưu Refresh Token vào Cookie an toàn (HttpOnly)
        SetRefreshTokenCookie(refreshToken);

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        // Lấy Refresh Token từ Cookie
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Không tìm thấy Refresh Token." });

        var (result, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
        if (result == null || newRefreshToken == null)
            return Unauthorized(new { message = "Refresh Token không hợp lệ hoặc đã hết hạn." });

        // Cập nhật lại Cookie mới
        SetRefreshTokenCookie(newRefreshToken);

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
        }

        // Xóa Cookie
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Đăng xuất thành công." });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Chống XSS (JavaScript frontend không thể truy cập Cookie này)
            Secure = true,   // Bắt buộc truyền qua HTTPS (trên Dev HTTP có thể đặt false nếu cần)
            SameSite = SameSiteMode.Strict, // Chống tấn công CSRF
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}