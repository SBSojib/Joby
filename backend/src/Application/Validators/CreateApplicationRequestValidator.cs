using FluentValidation;
using Joby.Application.DTOs.Applications;

namespace Joby.Application.Validators;

public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID is required");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Invalid email format");
    }
}

public class AddApplicationEventRequestValidator : AbstractValidator<AddApplicationEventRequest>
{
    public AddApplicationEventRequestValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty().WithMessage("Event type is required")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(2000);
    }
}





