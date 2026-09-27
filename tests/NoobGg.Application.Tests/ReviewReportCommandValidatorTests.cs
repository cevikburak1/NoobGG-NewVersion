using NoobGg.Application.Features.Moderation.Commands.ReviewReport;
using Xunit;

namespace NoobGg.Application.Tests;

public class ReviewReportCommandValidatorTests
{
    private readonly ReviewReportCommandValidator _validator = new();

    [Theory]
    [InlineData("warn")]
    [InlineData("ban")]
    [InlineData("dismiss")]
    public void Supported_actions_are_accepted(string action)
    {
        var command = new ReviewReportCommand { ReportId = "report-1", Action = action, Reason = "Reviewed" };
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("resolved")]
    [InlineData("delete")]
    public void Unsupported_actions_are_rejected(string action)
    {
        var command = new ReviewReportCommand { ReportId = "report-1", Action = action, Reason = "Reviewed" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Sanction_requires_reason()
    {
        var command = new ReviewReportCommand { ReportId = "report-1", Action = "ban" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Suspension_requires_future_end_time()
    {
        var missing = new ReviewReportCommand { ReportId = "report-1", Action = "suspend", Reason = "Reviewed" };
        var past = missing with { EndsAt = DateTime.UtcNow.AddMinutes(-1) };
        var future = missing with { EndsAt = DateTime.UtcNow.AddDays(1) };
        Assert.False(_validator.Validate(missing).IsValid);
        Assert.False(_validator.Validate(past).IsValid);
        Assert.True(_validator.Validate(future).IsValid);
    }
}
