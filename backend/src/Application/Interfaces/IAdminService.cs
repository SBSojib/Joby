using Joby.Application.DTOs.Admin;

namespace Joby.Application.Interfaces;

public interface IAdminService
{
    Task<List<AdminUserDto>> GetAllUsersAsync(Guid requesterUserId);
    Task DeleteUserAsync(Guid requesterUserId, Guid targetUserId);
}
