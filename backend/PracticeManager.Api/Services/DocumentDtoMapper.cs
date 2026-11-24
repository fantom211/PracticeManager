using PracticeManager.Api.Contracts;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Domain.Enums;

namespace PracticeManager.Api.Services;

/// <summary>
/// Pure mapping from document entities to API DTOs. Keeping this outside the endpoints makes the
/// soft-delete, canDelete and compatibility teacher-comment rules unit-testable.
/// </summary>
public static class DocumentDtoMapper
{
    private static readonly DocumentStatusPolicy Policy = new();

    public static List<TeacherCommentDto> ToCommentDtos(IEnumerable<TeacherComment> comments)
    {
        return comments
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Select(c => new TeacherCommentDto(
                c.Id, c.DocumentId, c.VersionNumber, c.AuthorName, c.AuthorRole,
                c.Kind, c.Summary, c.FullText, c.CreatedAt))
            .ToList();
    }

    /// <summary>
    /// TeacherComment.Summary is authoritative; the legacy DocumentVersion.TeacherComment field is
    /// only a fallback for documents that predate the teacher_comments table.
    /// </summary>
    public static string? ResolveTeacherComment(Document document, IEnumerable<TeacherComment>? comments = null)
    {
        var summary = (comments ?? document.Comments)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .FirstOrDefault()?.Summary;

        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary;
        }

        return document.Versions.Count > 0
            ? document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.TeacherComment
            : document.CurrentVersion?.TeacherComment;
    }

    public static DocumentListItem ToListItem(Document document, IEnumerable<TeacherComment>? comments = null)
    {
        var (canGenerate, canUpload, canSubmit) = ComputePermissions(document.Status);
        return new DocumentListItem(
            document.Id,
            document.Name,
            document.DocumentType!.Code,
            document.DocumentType.Name,
            StatusLabels.CamelCase(document.Status),
            StatusLabels.Document(document.Status),
            document.Status == DocumentStatus.NotUploaded ? null : document.UpdatedAt,
            ResolveTeacherComment(document, comments),
            document.CurrentVersion?.VersionNumber,
            canGenerate,
            canUpload,
            canSubmit,
            CanDelete(document));
    }

    public static DocumentDetailResponse ToDetail(Document document)
    {
        var (canGenerate, canUpload, canSubmit) = ComputePermissions(document.Status);
        var versions = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionDto(
                v.Id, v.VersionNumber, StatusLabels.CamelCase(v.Source), StatusLabels.Source(v.Source),
                v.FileName, v.SizeBytes, v.CreatedAt, v.TeacherComment,
                $"/api/documents/{document.Id}/download?version={v.VersionNumber}"))
            .ToList();

        var comments = ToCommentDtos(document.Comments);
        var hasFile = document.CurrentVersionId != null || document.CurrentVersion != null;

        return new DocumentDetailResponse(
            document.Id,
            document.Name,
            document.DocumentType!.Code,
            document.DocumentType.Name,
            StatusLabels.CamelCase(document.Status),
            StatusLabels.Document(document.Status),
            document.Content,
            document.Status == DocumentStatus.NotUploaded ? null : document.UpdatedAt,
            ResolveTeacherComment(document, document.Comments),
            versions,
            canGenerate,
            canUpload,
            canSubmit,
            hasFile ? $"/api/documents/{document.Id}/preview" : null,
            hasFile ? $"/api/documents/{document.Id}/download" : null,
            CanDelete(document),
            comments);
    }

    public static bool CanDelete(Document document) => document.DeletedAt == null && Policy.CanDelete(document.Status);

    private static (bool CanGenerate, bool CanUpload, bool CanSubmit) ComputePermissions(DocumentStatus status) => status switch
    {
        DocumentStatus.NotUploaded => (true, true, false),
        DocumentStatus.Draft => (true, true, true),
        DocumentStatus.Returned => (true, true, false),
        DocumentStatus.Submitted => (false, false, false),
        DocumentStatus.Accepted => (false, false, false),
        _ => (false, false, false)
    };
}
