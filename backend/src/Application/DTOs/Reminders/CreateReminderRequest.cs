namespace Joby.Application.DTOs.Reminders;

public class CreateReminderRequest
{
    public Guid? ApplicationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueAt { get; set; }
    public string? ReminderType { get; set; }
}

public class SnoozeReminderRequest
{
    public DateTime SnoozedUntil { get; set; }
}





