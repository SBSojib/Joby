using System.Security.Claims;
using Joby.Application.DTOs.Admin;
using Joby.Application.DTOs.Auth;
using Joby.Application.Interfaces;
using Joby.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAuthService _authService;

    public AdminController(IAdminService adminService, IAuthService authService)
    {
        _adminService = adminService;
        _authService = authService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
    {
        var requesterUserId = GetAdminRequesterUserId();
        var users = await _adminService.GetAllUsersAsync(requesterUserId);
        return Ok(users);
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var requesterUserId = GetAdminRequesterUserId();
        await _adminService.DeleteUserAsync(requesterUserId, userId);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/impersonate")]
    public async Task<ActionResult<AuthResponse>> Impersonate(Guid userId)
    {
        var adminId = GetAdminRequesterUserId();
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var ipAddress = GetIpAddress();
        var response = await _authService.StartImpersonationAsync(adminId, userId, refreshToken, ipAddress);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    private Guid GetAdminRequesterUserId()
    {
        var impersonator = User.FindFirst(JwtClaimTypes.Impersonator)?.Value;
        if (!string.IsNullOrEmpty(impersonator) && Guid.TryParse(impersonator, out var adminId))
        {
            return adminId;
        }

        return GetSubjectUserId();
    }

    private Guid GetSubjectUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
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
}
