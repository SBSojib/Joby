using Joby.Application.DTOs.Auth;

namespace Joby.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterPendingResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, string ipAddress);
    Task ResendVerificationCodeAsync(ResendVerificationRequest request);
    Task RequestPasswordResetAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task DeleteAccountAsync(Guid userId, string password, string ipAddress);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task<AuthResponse> StartImpersonationAsync(Guid adminUserId, Guid targetUserId, string refreshToken, string ipAddress);
    Task<AuthResponse> StopImpersonationAsync(string refreshToken, string ipAddress);
}





