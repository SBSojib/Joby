using FluentValidation;
using Joby.Application.DTOs.Profile;

namespace Joby.Application.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format");
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(4000);
        RuleFor(x => x.CurrentTitle).MaximumLength(200);
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 80).When(x => x.YearsOfExperience.HasValue);
        RuleFor(x => x.Skills).Must(x => x == null || x.Count <= 100).WithMessage("Skills cannot exceed 100 items");
        RuleFor(x => x.Keywords).Must(x => x == null || x.Count <= 100).WithMessage("Keywords cannot exceed 100 items");
        RuleForEach(x => x.Skills).MaximumLength(100);
        RuleForEach(x => x.Keywords).MaximumLength(100);
        RuleFor(x => x.PreferredLocations).MaximumLength(1000);
        RuleFor(x => x.PreferredJobTypes).MaximumLength(500);
        RuleFor(x => x.MinSalary).GreaterThanOrEqualTo(0).When(x => x.MinSalary.HasValue);
        RuleFor(x => x.MaxSalary).GreaterThanOrEqualTo(0).When(x => x.MaxSalary.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinSalary.HasValue || !x.MaxSalary.HasValue || x.MinSalary <= x.MaxSalary)
            .WithMessage("Minimum salary cannot be greater than maximum salary");
        RuleFor(x => x.WorkExperience).Must(x => x == null || x.Count <= 50).WithMessage("Work experience cannot exceed 50 items");
        RuleFor(x => x.Education).Must(x => x == null || x.Count <= 50).WithMessage("Education cannot exceed 50 items");
        RuleForEach(x => x.WorkExperience).SetValidator(new WorkExperienceDtoValidator());
        RuleForEach(x => x.Education).SetValidator(new EducationDtoValidator());
    }
}

public class WorkExperienceDtoValidator : AbstractValidator<WorkExperienceDto>
{
    public WorkExperienceDtoValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Company).MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.StartDate).MaximumLength(50);
        RuleFor(x => x.EndDate).MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class EducationDtoValidator : AbstractValidator<EducationDto>
{
    public EducationDtoValidator()
    {
        RuleFor(x => x.Degree).MaximumLength(200);
        RuleFor(x => x.School).MaximumLength(200);
        RuleFor(x => x.Field).MaximumLength(200);
        RuleFor(x => x.StartDate).MaximumLength(50);
        RuleFor(x => x.EndDate).MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
