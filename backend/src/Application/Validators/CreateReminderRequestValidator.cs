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
            .NotEmpty().WithMessage("Due date is required")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5)).WithMessage("Due date must be in the future");

        RuleFor(x => x.Description)
            .MaximumLength(2000);
    }
}





