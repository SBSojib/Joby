using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Joby.Application.DTOs.Auth;
using Joby.Application.Interfaces;
using Joby.Application.Security;
using Joby.Domain.Entities;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Joby.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailSender emailSender,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<RegisterPendingResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            throw new InvalidOperationException("Email is already registered");
        }

        var (verificationCode, codeHash, expiresAt) = GenerateVerificationCode();
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsEmailVerified = false,
            EmailVerificationCodeHash = codeHash,
            EmailVerificationCodeExpiresAt = expiresAt,
            EmailVerificationCodeSentAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var sent = await SendVerificationCodeEmailAsync(user.Email, verificationCode);
        if (!sent)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Could not send verification email. Please check email settings and try again.");
        }

        return new RegisterPendingResponse
        {
            Email = user.Email,
            Message = "Verification code sent to your email. Please verify to continue."
        };
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, string ipAddress)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            throw new KeyNotFoundException("Account not found");
        }

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Email is already verified");
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) || user.EmailVerificationCodeExpiresAt == null)
        {
            throw new InvalidOperationException("Verification code is not available. Please request a new code.");
        }

        if (user.EmailVerificationCodeExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Verification code has expired. Please request a new code.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, user.EmailVerificationCodeHash))
        {
            throw new UnauthorizedAccessException("Invalid verification code");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAt = null;
        user.EmailVerificationCodeSentAt = null;
        user.LastLoginAt = DateTime.UtcNow;

        if (user.Profile == null)
        {
            _context.Profiles.Add(new Profile
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}"
            });
        }

        await _context.SaveChangesAsync();
        return await GenerateAuthResponse(user, ipAddress);
    }

    public async Task ResendVerificationCodeAsync(ResendVerificationRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            throw new KeyNotFoundException("Account not found");
        }

        if (user.IsEmailVerified)
        {
            throw new InvalidOperationException("Email is already verified");
        }

        var cooldownSeconds = GetVerificationResendCooldownSeconds();
        if (user.EmailVerificationCodeSentAt.HasValue
            && user.EmailVerificationCodeSentAt.Value.AddSeconds(cooldownSeconds) > DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Please wait {cooldownSeconds} seconds before requesting another code.");
        }

        var (verificationCode, codeHash, expiresAt) = GenerateVerificationCode();
        user.EmailVerificationCodeHash = codeHash;
        user.EmailVerificationCodeExpiresAt = expiresAt;
        user.EmailVerificationCodeSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var sent = await SendVerificationCodeEmailAsync(user.Email, verificationCode);
        if (!sent)
        {
            throw new InvalidOperationException("Could not send verification email. Please check email settings and try again.");
        }
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return;
        }

        var cooldownSeconds = GetPasswordResetResendCooldownSeconds();
        if (user.PasswordResetCodeSentAt.HasValue
            && user.PasswordResetCodeSentAt.Value.AddSeconds(cooldownSeconds) > DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Please wait {cooldownSeconds} seconds before requesting another code.");
        }

        var (resetCode, codeHash, expiresAt) = GeneratePasswordResetCode();
        user.PasswordResetCodeHash = codeHash;
        user.PasswordResetCodeExpiresAt = expiresAt;
        user.PasswordResetCodeSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var sent = await SendPasswordResetCodeEmailAsync(user.Email, resetCode);
        if (!sent)
        {
            throw new InvalidOperationException("Could not send reset code email. Please check email settings and try again.");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or reset code");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) || user.PasswordResetCodeExpiresAt == null)
        {
            throw new InvalidOperationException("Reset code is not available. Please request a new code.");
        }

        if (user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Reset code has expired. Please request a new code.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, user.PasswordResetCodeHash))
        {
            throw new UnauthorizedAccessException("Invalid email or reset code");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAt = null;
        user.PasswordResetCodeSentAt = null;

        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = "Password reset";
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Guid userId, string password, string ipAddress)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid password");
        }

        _logger.LogInformation("Deleting account for user {UserId} from IP {IpAddress}", userId, ipAddress);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Email is not verified. Please verify your email first.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GenerateAuthResponse(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        if (!token.IsActive)
        {
            throw new UnauthorizedAccessException("Token is expired or revoked");
        }

        // Revoke the old token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Replaced by new token";

        var newRefreshToken = GenerateRefreshToken(ipAddress);
        newRefreshToken.UserId = token.UserId;
        newRefreshToken.ImpersonatedUserId = token.ImpersonatedUserId;
        token.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        User subjectUser = token.User;
        Guid? impersonatorId = null;
        string? impersonatorEmail = null;
        if (token.ImpersonatedUserId is { } impersonatedId)
        {
            var impersonated = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == impersonatedId);
            if (impersonated == null)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            subjectUser = impersonated;
            impersonatorId = token.UserId;
            impersonatorEmail = token.User.Email;
        }

        var accessToken = GenerateAccessToken(subjectUser, impersonatorId);
        var expiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(subjectUser, impersonatorId, impersonatorEmail)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Revoked by user";

        await _context.SaveChangesAsync();
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        return MapToUserDto(user);
    }

    public async Task<AuthResponse> StartImpersonationAsync(
        Guid adminUserId,
        Guid targetUserId,
        string refreshToken,
        string ipAddress)
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
        if (admin == null)
        {
            throw new UnauthorizedAccessException("Only root admin can impersonate users.");
        }

        if (!IsRootAdmin(admin))
        {
            throw new UnauthorizedAccessException("Only root admin can impersonate users.");
        }

        var target = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUserId);
        if (target == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        if (token.UserId != adminUserId)
        {
            throw new UnauthorizedAccessException("Session does not match admin account.");
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Impersonation started";

        var newRefreshToken = GenerateRefreshToken(ipAddress);
        newRefreshToken.UserId = adminUserId;
        newRefreshToken.ImpersonatedUserId = targetUserId;
        token.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(target, adminUserId);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            User = MapToUserDto(target, adminUserId, admin.Email)
        };
    }

    public async Task<AuthResponse> StopImpersonationAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        if (token.ImpersonatedUserId == null)
        {
            throw new InvalidOperationException("Not in an impersonation session.");
        }

        var admin = token.User;
        if (!IsRootAdmin(admin))
        {
            throw new UnauthorizedAccessException("Unauthorized.");
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = "Impersonation ended";

        var newRefreshToken = GenerateRefreshToken(ipAddress);
        newRefreshToken.UserId = token.UserId;
        newRefreshToken.ImpersonatedUserId = null;
        token.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(admin, null);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            User = MapToUserDto(admin)
        };
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user, string ipAddress)
    {
        var accessToken = GenerateAccessToken(user, null);
        var refreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.UserId = user.Id;

        // Remove old refresh tokens
        var oldTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && (t.RevokedAt != null || t.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(oldTokens);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            User = MapToUserDto(user)
        };
    }

    private string GenerateAccessToken(User user, Guid? impersonatorUserId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimList = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (impersonatorUserId.HasValue)
        {
            claimList.Add(new Claim(JwtClaimTypes.Impersonator, impersonatorUserId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claimList,
            expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(string ipAddress)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays()),
            CreatedByIp = ipAddress
        };
    }

    private int GetAccessTokenExpirationMinutes() =>
        int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

    private int GetRefreshTokenExpirationDays() =>
        int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

    private int GetVerificationCodeExpirationMinutes() =>
        int.Parse(_configuration["Auth:EmailVerificationCodeExpirationMinutes"] ?? "10");

    private int GetVerificationResendCooldownSeconds() =>
        int.Parse(_configuration["Auth:EmailVerificationResendCooldownSeconds"] ?? "60");

    private int GetPasswordResetCodeExpirationMinutes() =>
        int.Parse(_configuration["Auth:PasswordResetCodeExpirationMinutes"] ?? "10");

    private int GetPasswordResetResendCooldownSeconds() =>
        int.Parse(_configuration["Auth:PasswordResetResendCooldownSeconds"] ?? "60");

    private (string Code, string CodeHash, DateTime ExpiresAt) GenerateVerificationCode()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var codeHash = BCrypt.Net.BCrypt.HashPassword(code);
        var expiresAt = DateTime.UtcNow.AddMinutes(GetVerificationCodeExpirationMinutes());
        return (code, codeHash, expiresAt);
    }

    private (string Code, string CodeHash, DateTime ExpiresAt) GeneratePasswordResetCode()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var codeHash = BCrypt.Net.BCrypt.HashPassword(code);
        var expiresAt = DateTime.UtcNow.AddMinutes(GetPasswordResetCodeExpirationMinutes());
        return (code, codeHash, expiresAt);
    }

    private async Task<bool> SendVerificationCodeEmailAsync(string email, string code)
    {
        var subject = "Verify your Joby account";
        var textBody =
            $"Your Joby verification code is {code}. " +
            $"It expires in {GetVerificationCodeExpirationMinutes()} minutes.";

        var sent = await _emailSender.TrySendAsync(email, subject, textBody);
        if (!sent)
        {
            _logger.LogWarning("Verification email could not be sent to {Email}", email);
        }
        return sent;
    }

    private async Task<bool> SendPasswordResetCodeEmailAsync(string email, string code)
    {
        var subject = "Reset your Joby password";
        var textBody =
            $"Your Joby password reset code is {code}. " +
            $"It expires in {GetPasswordResetCodeExpirationMinutes()} minutes.";

        var sent = await _emailSender.TrySendAsync(email, subject, textBody);
        if (!sent)
        {
            _logger.LogWarning("Password reset email could not be sent to {Email}", email);
        }

        return sent;
    }

    private UserDto MapToUserDto(User user, Guid? impersonatorUserId = null, string? impersonatorEmail = null) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsAdmin = user.Email == GetRootAdminEmail(),
        DefaultFollowUpDays = user.DefaultFollowUpDays,
        ImpersonatorUserId = impersonatorUserId,
        ImpersonatorEmail = impersonatorEmail
    };

    private string GetRootAdminEmail() =>
        (_configuration["Admin:RootEmail"] ?? string.Empty).Trim().ToLowerInvariant();

    private bool IsRootAdmin(User user) =>
        user.Email == GetRootAdminEmail();
}





