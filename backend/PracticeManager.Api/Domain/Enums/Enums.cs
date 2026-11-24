namespace PracticeManager.Api.Domain.Enums;

public enum DocumentStatus
{
    NotUploaded,
    Draft,
    Submitted,
    Returned,
    Accepted
}

public enum ParticipantStatus
{
    Assigned,
    NotStarted,
    InProgress,
    Completed,
    Failed
}

public enum TaskStatus
{
    Assigned,
    InProgress,
    Submitted,
    Accepted,
    Returned
}

public enum StageStatus
{
    NotStarted,
    InProgress,
    Completed
}

public enum AssessmentResult
{
    NotAssessed,
    Passed,
    Failed
}

public enum DocumentSource
{
    Generated,
    Uploaded,
    Seed
}
