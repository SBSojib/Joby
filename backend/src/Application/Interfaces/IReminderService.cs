using Joby.Application.DTOs.Applications;
using Joby.Application.DTOs.Reminders;

namespace Joby.Application.Interfaces;

public interface IReminderService
{
    Task<ReminderDto> CreateReminderAsync(Guid userId, CreateReminderRequest request);
    Task<List<ReminderDto>> GetRemindersAsync(Guid userId, bool includePast = false);
    Task<List<ReminderDto>> GetUpcomingRemindersAsync(Guid userId, int days = 7);
    Task<ReminderDto> SnoozeReminderAsync(Guid userId, Guid reminderId, SnoozeReminderRequest request);
    Task<ReminderDto> CompleteReminderAsync(Guid userId, Guid reminderId);
    Task<ReminderDto> DismissReminderAsync(Guid userId, Guid reminderId);
    Task CreateFollowUpReminderAsync(Guid userId, Guid applicationId, int daysAfterApplied);
}





