using FluentValidation;
using Joby.Application.DTOs.Auth;

namespace Joby.Application.Validators;

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
