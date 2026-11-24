namespace PracticeManager.Api.Contracts;

public record DocumentListItem(
    Guid Id,
    string Name,
    string TypeCode,
    string TypeName,
    string Status,
    string StatusLabel,
    DateTimeOffset? UpdatedAt,
    string? TeacherComment,
    int? CurrentVersionNumber,
    bool CanGenerate,
    bool CanUpload,
    bool CanSubmit,
    bool CanDelete
);

public record DocumentVersionDto(
    Guid Id,
    int VersionNumber,
    string Source,
    string SourceLabel,
    string FileName,
    long SizeBytes,
    DateTimeOffset CreatedAt,
    string? TeacherComment,
    string DownloadUrl
);

public record TeacherCommentDto(
    Guid Id,
    Guid DocumentId,
    int? VersionNumber,
    string AuthorName,
    string AuthorRole,
    string Kind,
    string Summary,
    string FullText,
    DateTimeOffset CreatedAt
);

public record DocumentDetailResponse(
    Guid Id,
    string Name,
    string TypeCode,
    string TypeName,
    string Status,
    string StatusLabel,
    string? Content,
    DateTimeOffset? UpdatedAt,
    string? TeacherComment,
    List<DocumentVersionDto> Versions,
    bool CanGenerate,
    bool CanUpload,
    bool CanSubmit,
    string? PreviewUrl,
    string? DownloadUrl,
    bool CanDelete,
    List<TeacherCommentDto> Comments
);

public record GenerateDocumentRequest(Guid? TemplateId);

public record GenerateDocumentResponse(DocumentDetailResponse Document, List<string> Warnings);

public record SaveContentRequest(string Content);

public record DocumentTemplateDto(
    Guid Id,
    string Code,
    string Name,
    string DocumentTypeCode,
    string? ProfessionalModule,
    int Version,
    string FileName,
    long SizeBytes,
    string DownloadUrl
);
