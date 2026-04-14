using Joby.Application.DTOs.Admin;
using Joby.Application.Interfaces;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Joby.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<AdminUserDto>> GetAllUsersAsync(Guid requesterUserId)
    {
        await EnsureRootAdminAsync(requesterUserId);

        var rootEmail = GetRootAdminEmail();
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsEmailVerified = u.IsEmailVerified,
                IsRootAdmin = u.Email == rootEmail,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return users;
    }

    public async Task DeleteUserAsync(Guid requesterUserId, Guid targetUserId)
    {
        var requester = await EnsureRootAdminAsync(requesterUserId);
        if (requester.Id == targetUserId)
        {
            throw new InvalidOperationException("Root admin account cannot delete itself.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (IsRootAdmin(user))
        {
            throw new InvalidOperationException("Root admin account cannot be deleted.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    private async Task<Domain.Entities.User> EnsureRootAdminAsync(Guid requesterUserId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == requesterUserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Unauthorized.");
        }

        if (!IsRootAdmin(user))
        {
            throw new UnauthorizedAccessException("Only root admin can access this resource.");
        }

        return user;
    }

    private bool IsRootAdmin(Domain.Entities.User user) =>
        user.Email == GetRootAdminEmail();

    private string GetRootAdminEmail() =>
        (_configuration["Admin:RootEmail"] ?? string.Empty).Trim().ToLowerInvariant();
}
