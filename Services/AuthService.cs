using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackendApp.Data;
using BackendApp.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace BackendApp.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    /*
     * HÀM: LÀM NHIỆM VỤ ĐĂNG NHẬP (LOGIN)
     * 1. Nhận thông tin đăng nhập từ Client (Username, Password).
     * 2. Tra cứu User trong Database kèm thông tin Role.
     * 3. Xác thực tài khoản: kiểm tra User có tồn tại, đang hoạt động (IsActive), và kiểm tra mật khẩu bằng BCrypt.Verify().
     * 4. Nếu hợp lệ: Sinh Access Token (JWT) chứa Claims dùng trong 15 phút.
     * 5. Sinh Refresh Token (chuỗi ngẫu nhiên 64 bytes mã hóa Base64) sống trong 7 ngày, lưu vào Database.
     * 6. Trả về Access Token cho Client và Refresh Token để lưu vào Cookie.
     */
    public async Task<(AuthResponseDto? Response, string? RefreshToken)> LoginAsync(LoginRequestDto dto)
    {
        // 1. Tìm employee theo Username kèm theo thông tin Role
        var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == dto.Username);

        // 2. Kiểm tra: nếu không thấy, hoặc đã thôi việc (TERMINATED), hoặc sai mật khẩu -> Từ chối
        if (employee == null || employee.Status == "TERMINATED" || !BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash))
            return (null, null);

        // 3. Tạo Access Token (JWT) ngắn hạn (ví dụ: 15 phút)
        var accessTokenExpires = DateTime.UtcNow.AddMinutes(15);
        var accessToken = GenerateJwtToken(employee, accessTokenExpires);

        // 4. Tạo Refresh Token dài hạn (ví dụ: 7 ngày)
        var refreshToken = GenerateRefreshToken();
        employee.RefreshToken = refreshToken;
        employee.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        // 5. Trả về DTO phản hồi
        var response = new AuthResponseDto(
            accessToken,
            employee.Id,
            employee.Username,
            employee.Email,
            employee.Role.Name,
            accessTokenExpires
        );
        return (response, refreshToken);
    }

    /*
         * GIA HẠN TOKEN (REFRESH TOKEN)
         * 1. Đọc Refresh Token cũ từ Cookie gửi lên.
         * 2. Tra cứu User trong Database theo Refresh Token.
         * 3. Kiểm tra Refresh Token hợp lệ và chưa hết hạn.
         * 4. Nếu hợp lệ: Sinh Access Token mới (JWT) chứa Claims dùng trong 15 phút.
         * 5. Sinh Refresh Token mới (Rotate) sống trong 7 ngày, lưu vào Database.
         * 6. Cập nhật Refresh Token mới vào Database và trả về cho Client.
    */
    public async Task<(AuthResponseDto? Response, string? NewRefreshToken)> RefreshTokenAsync(string oldRefreshToken)
    {
        var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.RefreshToken == oldRefreshToken);

        // Kiểm tra điều kiện gia hạn
        if (employee == null || employee.RefreshTokenExpiryTime <= DateTime.UtcNow || employee.Status == "TERMINATED")
            return (null, null);

        // Cấp Access Token mới và Xoay vòng (Rotate) Refresh Token mới
        var accessTokenExpires = DateTime.UtcNow.AddMinutes(15);
        var newAccessToken = GenerateJwtToken(employee, accessTokenExpires);
        var newRefreshToken = GenerateRefreshToken();

        employee.RefreshToken = newRefreshToken;
        employee.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto(
             newAccessToken,
             employee.Id,
             employee.Username,
             employee.Email,
             employee.Role.Name,
             accessTokenExpires
         );

        return (response, newRefreshToken);
    }

    /*
     * ĐĂNG XUẤT (REVOKE TOKEN)
     * 1. Đọc Refresh Token từ Cookie gửi lên.
     * 2. Tra cứu User trong Database theo Refresh Token.
     * 3. Xóa RefreshToken và RefreshTokenExpiryTime trong Database (gán về null).
     * 4. Xóa Cookie ở Client để hủy phiên đăng nhập.
    */
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.RefreshToken == refreshToken);
        if (employee == null) return false;

        employee.RefreshToken = null;
        employee.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private string GenerateJwtToken(Models.Employee employee, DateTime expiresAt)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Thêm thông tin vào Token Payload (Claims)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Name, employee.Username),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Role, employee.Role.Name),
            new Claim("EmployeeCode", employee.EmployeeCode), // Mã nhân viên EMP-001
            new Claim("FullName", employee.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}