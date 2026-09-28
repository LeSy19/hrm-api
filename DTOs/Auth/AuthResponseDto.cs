
namespace BackendApp.DTOs.Auth;

public record AuthResponseDto(
    string AccessToken,
    int UserId,
    string Username,
    string Email,
    string RoleName,
    DateTime ExpiresAt
);