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

        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.ContactPhone).MaximumLength(50);
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

public class UpdateApplicationStatusRequestValidator : AbstractValidator<UpdateApplicationStatusRequest>
{
    public UpdateApplicationStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}

public class UpdateApplicationRequestValidator : AbstractValidator<UpdateApplicationRequest>
{
    public UpdateApplicationRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.ContactPhone).MaximumLength(50);
        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail))
            .WithMessage("Invalid email format");
    }
}





