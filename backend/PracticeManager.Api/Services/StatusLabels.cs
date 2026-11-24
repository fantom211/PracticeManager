using PracticeManager.Api.Domain.Enums;

namespace PracticeManager.Api.Services;

/// <summary>
/// Russian display labels for domain enums. Enum values themselves are serialized as camelCase
/// strings; labels are only for UI display.
/// </summary>
public static class StatusLabels
{
    public static string Document(DocumentStatus status) => status switch
    {
        DocumentStatus.NotUploaded => "Не загружен",
        DocumentStatus.Draft => "Черновик",
        DocumentStatus.Submitted => "На проверке",
        DocumentStatus.Returned => "Возвращён на доработку",
        DocumentStatus.Accepted => "Принят",
        _ => status.ToString()
    };

    public static string Participant(ParticipantStatus status) => status switch
    {
        ParticipantStatus.Assigned => "Назначено",
        ParticipantStatus.NotStarted => "Не начата",
        ParticipantStatus.InProgress => "В процессе",
        ParticipantStatus.Completed => "Завершена",
        ParticipantStatus.Failed => "Не пройдена",
        _ => status.ToString()
    };

    public static string Stage(StageStatus status) => status switch
    {
        StageStatus.NotStarted => "Не начат",
        StageStatus.InProgress => "В процессе",
        StageStatus.Completed => "Завершён",
        _ => status.ToString()
    };

    public static string Task(Domain.Enums.TaskStatus status) => status switch
    {
        Domain.Enums.TaskStatus.Assigned => "Назначено",
        Domain.Enums.TaskStatus.InProgress => "В процессе",
        Domain.Enums.TaskStatus.Submitted => "На проверке",
        Domain.Enums.TaskStatus.Accepted => "Принято",
        Domain.Enums.TaskStatus.Returned => "Возвращено",
        _ => status.ToString()
    };

    public static string Result(AssessmentResult result) => result switch
    {
        AssessmentResult.NotAssessed => "Не оценено",
        AssessmentResult.Passed => "Зачтено",
        AssessmentResult.Failed => "Не зачтено",
        _ => result.ToString()
    };

    public static string Source(DocumentSource source) => source switch
    {
        DocumentSource.Generated => "Сгенерирован",
        DocumentSource.Uploaded => "Загружен",
        DocumentSource.Seed => "Сгенерирован",
        _ => source.ToString()
    };

    public static string CamelCase(Enum value) => char.ToLowerInvariant(value.ToString()[0]) + value.ToString()[1..];
}
