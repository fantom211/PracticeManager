using PracticeManager.Api.Domain.Enums;
using PracticeManager.Api.Services;

namespace PracticeManager.Tests;

public class DocumentStatusTests
{
    private readonly DocumentStatusPolicy _policy = new();

    [Theory]
    [InlineData(DocumentStatus.NotUploaded, DocumentTransitionTrigger.Generate, DocumentStatus.Draft)]
    [InlineData(DocumentStatus.NotUploaded, DocumentTransitionTrigger.Upload, DocumentStatus.Draft)]
    [InlineData(DocumentStatus.Draft, DocumentTransitionTrigger.Generate, DocumentStatus.Draft)]
    [InlineData(DocumentStatus.Draft, DocumentTransitionTrigger.Upload, DocumentStatus.Draft)]
    [InlineData(DocumentStatus.Draft, DocumentTransitionTrigger.Submit, DocumentStatus.Submitted)]
    [InlineData(DocumentStatus.Returned, DocumentTransitionTrigger.Generate, DocumentStatus.Draft)]
    [InlineData(DocumentStatus.Returned, DocumentTransitionTrigger.Upload, DocumentStatus.Draft)]
    public void Apply_AllowsValidTransitions(DocumentStatus from, DocumentTransitionTrigger trigger, DocumentStatus expected)
    {
        var result = _policy.Apply(from, trigger);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DocumentStatus.NotUploaded, DocumentTransitionTrigger.Submit)]
    [InlineData(DocumentStatus.Returned, DocumentTransitionTrigger.Submit)]
    [InlineData(DocumentStatus.Submitted, DocumentTransitionTrigger.Generate)]
    [InlineData(DocumentStatus.Submitted, DocumentTransitionTrigger.Upload)]
    [InlineData(DocumentStatus.Submitted, DocumentTransitionTrigger.Submit)]
    [InlineData(DocumentStatus.Accepted, DocumentTransitionTrigger.Generate)]
    [InlineData(DocumentStatus.Accepted, DocumentTransitionTrigger.Upload)]
    [InlineData(DocumentStatus.Accepted, DocumentTransitionTrigger.Submit)]
    public void Apply_RejectsInvalidTransitions_WithConflict(DocumentStatus from, DocumentTransitionTrigger trigger)
    {
        var ex = Assert.Throws<DocumentStatusConflictException>(() => _policy.Apply(from, trigger));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Equal(409, ex.StatusCode);
    }
}
