using FluentValidation;
using Joby.Application.DTOs.Jobs;

namespace Joby.Application.Validators;

public class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required")
            .MaximumLength(500);

        RuleFor(x => x.Company)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(500);

        RuleFor(x => x.Location)
            .MaximumLength(500);

        RuleFor(x => x.SourceUrl)
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.SourceUrl))
            .WithMessage("Invalid URL format");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public class CreateJobByUrlRequestValidator : AbstractValidator<CreateJobByUrlRequest>
{
    public CreateJobByUrlRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required")
            .Must(BeAValidUrl).WithMessage("Invalid URL format");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}





