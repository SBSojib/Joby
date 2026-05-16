using Joby.Application.DTOs.Jobs;
using Joby.Application.DTOs.Profile;
using Joby.Application.DTOs.Reminders;
using Joby.Application.Validators;
using Xunit;

namespace Joby.Tests;

public class RequestValidatorTests
{
    [Fact]
    public void JobSearchRequestValidator_RejectsUnboundedPageSize()
    {
        var validator = new JobSearchRequestValidator();

        var result = validator.Validate(new JobSearchRequest
        {
            Page = 0,
            PageSize = 1000
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(JobSearchRequest.Page));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(JobSearchRequest.PageSize));
    }

    [Fact]
    public void SnoozeReminderRequestValidator_RejectsPastSnoozeDate()
    {
        var validator = new SnoozeReminderRequestValidator();

        var result = validator.Validate(new SnoozeReminderRequest
        {
            SnoozedUntil = DateTime.UtcNow.AddMinutes(-1)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SnoozeReminderRequest.SnoozedUntil));
    }

    [Fact]
    public void UpdateProfileRequestValidator_RejectsInvalidSalaryRange()
    {
        var validator = new UpdateProfileRequestValidator();

        var result = validator.Validate(new UpdateProfileRequest
        {
            MinSalary = 150000,
            MaxSalary = 100000
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Minimum salary cannot be greater than maximum salary");
    }
}
