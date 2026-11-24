using Microsoft.AspNetCore.Http;
using PracticeManager.Api.Domain.Enums;

namespace PracticeManager.Api.Services;

public class DocumentStatusConflictException(string detail) : Exception(detail)
{
    public int StatusCode => StatusCodes.Status409Conflict;
}

public enum DocumentTransitionTrigger
{
    Generate,
    Upload,
    Submit
}

/// <summary>
/// Owns every document status transition. Endpoints must never set Document.Status directly.
/// </summary>
public class DocumentStatusPolicy
{
    public DocumentStatus Apply(DocumentStatus current, DocumentTransitionTrigger trigger)
    {
        return (current, trigger) switch
        {
            (DocumentStatus.NotUploaded, DocumentTransitionTrigger.Generate) => DocumentStatus.Draft,
            (DocumentStatus.NotUploaded, DocumentTransitionTrigger.Upload) => DocumentStatus.Draft,
            (DocumentStatus.Draft, DocumentTransitionTrigger.Generate) => DocumentStatus.Draft,
            (DocumentStatus.Draft, DocumentTransitionTrigger.Upload) => DocumentStatus.Draft,
            (DocumentStatus.Draft, DocumentTransitionTrigger.Submit) => DocumentStatus.Submitted,
            (DocumentStatus.Returned, DocumentTransitionTrigger.Generate) => DocumentStatus.Draft,
            (DocumentStatus.Returned, DocumentTransitionTrigger.Upload) => DocumentStatus.Draft,
            _ => throw new DocumentStatusConflictException(DescribeRejection(current, trigger))
        };
    }

    /// <summary>
    /// Only a returned document can be removed by the student. Deletion is always a soft delete.
    /// </summary>
    public bool CanDelete(DocumentStatus status) => status == DocumentStatus.Returned;

    public void EnsureCanDelete(DocumentStatus status)
    {
        if (CanDelete(status))
        {
            return;
        }

        throw new DocumentStatusConflictException(status switch
        {
            DocumentStatus.Submitted => "Документ на проверке не может быть удалён.",
            DocumentStatus.Accepted => "Принятый документ не может быть удалён.",
            DocumentStatus.Draft => "Черновик не может быть удалён.",
            DocumentStatus.NotUploaded => "Незагруженный документ не может быть удалён.",
            _ => "Документ не может быть удалён."
        });
    }

    private static string DescribeRejection(DocumentStatus current, DocumentTransitionTrigger trigger)
    {
        var action = trigger switch
        {
            DocumentTransitionTrigger.Generate => "сгенерировать документ",
            DocumentTransitionTrigger.Upload => "загрузить новую версию",
            DocumentTransitionTrigger.Submit => "отправить документ на проверку",
            _ => "выполнить действие"
        };
        var state = current switch
        {
            DocumentStatus.Submitted => "он находится на проверке",
            DocumentStatus.Accepted => "он уже принят и доступен только для просмотра",
            DocumentStatus.NotUploaded => "он не загружен",
            _ => "его текущий статус не позволяет это"
        };
        return $"Невозможно {action}: {state}.";
    }
}
