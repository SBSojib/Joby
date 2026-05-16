using FluentValidation;
using Joby.Application.DTOs.Reminders;

namespace Joby.Application.Validators;

public class CreateReminderRequestValidator : AbstractValidator<CreateReminderRequest>
{
    public CreateReminderRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500);

        RuleFor(x => x.DueAt)
            .NotEmpty().WithMessage("Due date and time are required")
            .Must(BeInTheFutureUtc)
            .WithMessage("Due date and time must be in the future");

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.ReminderType)
            .MaximumLength(100);

        RuleFor(x => x)
            .Must(x => x.ApplicationId.HasValue || x.JobId.HasValue)
            .WithMessage("Link the reminder to a job (jobId) or application (applicationId).");
    }

    private static bool BeInTheFutureUtc(DateTime dueAt)
    {
        var utc = dueAt.Kind switch
        {
            DateTimeKind.Utc => dueAt,
            DateTimeKind.Local => dueAt.ToUniversalTime(),
            // Client sends ISO with Z; STJ typically marks Utc. If unspecified, treat as UTC instant.
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dueAt, DateTimeKind.Utc),
            _ => dueAt.ToUniversalTime()
        };
        return utc > DateTime.UtcNow.AddMinutes(-5);
    }
}

public class SnoozeReminderRequestValidator : AbstractValidator<SnoozeReminderRequest>
{
    public SnoozeReminderRequestValidator()
    {
        RuleFor(x => x.SnoozedUntil)
            .NotEmpty().WithMessage("Snooze date and time are required")
            .Must(BeInTheFutureUtc)
            .WithMessage("Snooze date and time must be in the future");
    }

    private static bool BeInTheFutureUtc(DateTime snoozedUntil)
    {
        var utc = snoozedUntil.Kind switch
        {
            DateTimeKind.Utc => snoozedUntil,
            DateTimeKind.Local => snoozedUntil.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(snoozedUntil, DateTimeKind.Utc),
            _ => snoozedUntil.ToUniversalTime()
        };
        return utc > DateTime.UtcNow;
    }
}





