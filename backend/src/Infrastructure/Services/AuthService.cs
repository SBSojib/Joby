using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Joby.Application.DTOs.Auth;
using Joby.Application.Interfaces;
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

        await SendVerificationCodeEmailAsync(user.Email, verificationCode);

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

        await SendVerificationCodeEmailAsync(user.Email, verificationCode);
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
        token.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(token.User);
        var expiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(token.User)
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

    private async Task<AuthResponse> GenerateAuthResponse(User user, string ipAddress)
    {
        var accessToken = GenerateAccessToken(user);
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

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
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

    private (string Code, string CodeHash, DateTime ExpiresAt) GenerateVerificationCode()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var codeHash = BCrypt.Net.BCrypt.HashPassword(code);
        var expiresAt = DateTime.UtcNow.AddMinutes(GetVerificationCodeExpirationMinutes());
        return (code, codeHash, expiresAt);
    }

    private async Task SendVerificationCodeEmailAsync(string email, string code)
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
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DefaultFollowUpDays = user.DefaultFollowUpDays
    };
}





