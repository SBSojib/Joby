using Joby.Application.DTOs.Auth;
using Joby.Application.Interfaces;
using Joby.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterPendingResponse>> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(response);
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var ipAddress = GetIpAddress();
        var response = await _authService.VerifyEmailAsync(request, ipAddress);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        await _authService.ResendVerificationCodeAsync(request);
        return Ok(new { message = "Verification code sent." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.RequestPasswordResetAsync(request);
        return Ok(new { message = "If an account exists for this email, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Password reset successful. You can now sign in." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var response = await _authService.LoginAsync(request, ipAddress);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var ipAddress = GetIpAddress();
        var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = GetIpAddress();
            await _authService.RevokeTokenAsync(refreshToken, ipAddress);
        }

        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = GetUserId();
        var ipAddress = GetIpAddress();
        await _authService.DeleteAccountAsync(userId, request.Password, ipAddress);
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Account deleted successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetUserId();
        var user = await _authService.GetCurrentUserAsync(userId);
        var impersonator = User.FindFirst(JwtClaimTypes.Impersonator)?.Value;
        if (!string.IsNullOrEmpty(impersonator) && Guid.TryParse(impersonator, out var impersonatorId))
        {
            var admin = await _authService.GetCurrentUserAsync(impersonatorId);
            user.ImpersonatorUserId = impersonatorId;
            user.ImpersonatorEmail = admin.Email;
        }

        return Ok(user);
    }

    [HttpPost("stop-impersonation")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> StopImpersonation()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var ipAddress = GetIpAddress();
        var response = await _authService.StopImpersonationAsync(refreshToken, ipAddress);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict,
            Secure = true
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString();
        }

        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}


