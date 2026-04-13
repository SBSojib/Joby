using Hangfire;
using Joby.Application.Interfaces;
using Joby.Domain.Enums;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Joby.Infrastructure.BackgroundJobs;

public class ReminderEmailDispatchJob
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReminderEmailDispatchJob> _logger;

    public ReminderEmailDispatchJob(
        ApplicationDbContext db,
        IEmailSender emailSender,
        ILogger<ReminderEmailDispatchJob> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    [DisableConcurrentExecution(120)]
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        var ids = await _db.Reminders.AsNoTracking()
            .Where(r =>
                (r.Status == ReminderStatus.Pending || r.Status == ReminderStatus.Snoozed) &&
                r.DueAt <= now &&
                r.EmailSentAt == null)
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var id in ids)
        {
            var reminder = await _db.Reminders
                .Include(r => r.User)
                .Include(r => r.Application)
                    .ThenInclude(a => a!.Job)
                .Include(r => r.Job)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.EmailSentAt == null &&
                    (r.Status == ReminderStatus.Pending || r.Status == ReminderStatus.Snoozed) &&
                    r.DueAt <= DateTime.UtcNow);

            if (reminder == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(reminder.User.Email))
            {
                _logger.LogWarning("Reminder {ReminderId}: user email missing", reminder.Id);
                continue;
            }

            var jobLabel = reminder.Application?.Job != null
                ? $"{reminder.Application.Job.Title} at {reminder.Application.Job.Company}"
                : reminder.Job != null
                    ? $"{reminder.Job.Title} at {reminder.Job.Company}"
                    : "your job tracker";

            var body = $"Reminder: {reminder.Title}\n\n" +
                       $"Job: {jobLabel}\n" +
                       $"Due (UTC): {reminder.DueAt:u}\n\n" +
                       (string.IsNullOrWhiteSpace(reminder.Description) ? "" : reminder.Description + "\n\n") +
                       "— Joby";

            var sent = await _emailSender.TrySendAsync(
                reminder.User.Email,
                $"Joby reminder: {reminder.Title}",
                body);

            if (sent)
            {
                reminder.EmailSentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Sent reminder email for {ReminderId} to {Email}", reminder.Id, reminder.User.Email);
            }
        }
    }
}
