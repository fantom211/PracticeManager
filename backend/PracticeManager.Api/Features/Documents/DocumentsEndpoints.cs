using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Contracts;
using PracticeManager.Api.Data;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Domain.Enums;
using PracticeManager.Api.Services;
using PracticeManager.Api.Services.DocumentGeneration;

namespace PracticeManager.Api.Features.Documents;

public static class DocumentsEndpoints
{
    public static void MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapGet("/", async (string? status, Guid? participantId, AppDbContext db, CurrentStudentProvider current) =>
        {
            IQueryable<Document> query = DocumentQueries.VisibleForStudent(db, current.StudentId, participantId)
                .Include(d => d.DocumentType)
                .Include(d => d.CurrentVersion)
                .Include(d => d.Comments)
                .AsSplitQuery();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(d => d.Status == parsedStatus);
            }

            var documents = await query.OrderBy(d => d.DocumentType!.Name).ToListAsync();
            return Results.Ok(documents.Select(d => DocumentDtoMapper.ToListItem(d)).ToList());
        })
        .WithName("GetDocuments");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CurrentStudentProvider current) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            return Results.Ok(DocumentDtoMapper.ToDetail(document));
        })
        .WithName("GetDocumentDetail");

        group.MapGet("/{id:guid}/comments", async (Guid id, AppDbContext db, CurrentStudentProvider current) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            return Results.Ok(DocumentDtoMapper.ToCommentDtos(document.Comments));
        })
        .WithName("GetDocumentComments");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CurrentStudentProvider current, DocumentStatusPolicy statusPolicy) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            try
            {
                statusPolicy.EnsureCanDelete(document.Status);
            }
            catch (DocumentStatusConflictException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: ex.StatusCode);
            }

            // Soft delete only: versions and files stay on disk and in the database for audit.
            document.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteDocument");

        group.MapGet("/{id:guid}/download", async (Guid id, int? version, AppDbContext db, CurrentStudentProvider current, FileStorage storage) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            var target = ResolveVersion(document, version);
            if (target == null)
            {
                return Results.NotFound(new { detail = "Версия документа не найдена." });
            }

            var bytes = storage.ReadVersion(target.FilePath);
            return ResultsWithFileName(bytes, target.ContentType, target.FileName, inline: false);
        })
        .WithName("DownloadDocument");

        group.MapGet("/{id:guid}/preview", async (Guid id, int? version, AppDbContext db, CurrentStudentProvider current, FileStorage storage) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            var target = ResolveVersion(document, version);
            if (target == null)
            {
                return Results.NotFound(new { detail = "Версия документа не найдена." });
            }

            var bytes = storage.ReadVersion(target.FilePath);
            return ResultsWithFileName(bytes, target.ContentType, target.FileName, inline: true);
        })
        .WithName("PreviewDocument");

        group.MapPost("/{id:guid}/generate", async (Guid id, GenerateDocumentRequest? request, AppDbContext db, CurrentStudentProvider current,
            FileStorage storage, DocxTemplateRenderer renderer, DocumentStatusPolicy statusPolicy, ILogger<Program> logger) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            document.Status = statusPolicy.Apply(document.Status, DocumentTransitionTrigger.Generate);

            var template = request?.TemplateId != null
                ? await db.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId)
                : await ResolveActiveTemplate(db, document);

            if (template == null)
            {
                return Results.NotFound(new { detail = "Активный шаблон документа не найден." });
            }

            if (!storage.TryResolveTemplatePath(template.FilePath, out var templatePath) || !File.Exists(templatePath))
            {
                logger.LogWarning("Template file missing for template {Code}", template.Code);
                return Results.Problem(detail: "Файл шаблона не найден на диске.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var templateBytes = await File.ReadAllBytesAsync(templatePath);
            var participant = document.Participant!;
            var diaryEntries = TemplateValueBuilder.BuildDiaryRows(participant.DiaryEntries.ToList());
            var taskRows = TemplateValueBuilder.BuildTaskRows(participant.Tasks.ToList());
            var values = TemplateValueBuilder.Build(participant, document);

            var result = renderer.Render(templateBytes, values, new List<RepeatingBlockData>
            {
                new("diary_rows", diaryEntries),
                new("task_rows", taskRows)
            });

            var nextVersionNumber = document.Versions.Count == 0 ? 1 : document.Versions.Max(v => v.VersionNumber) + 1;
            var fileName = $"{document.DocumentType!.Code}-{SlugifyStudent(participant.Student!)}-v{nextVersionNumber}.docx";
            var (relativePath, size, sha) = storage.SaveVersion(document.Id, nextVersionNumber, result.Content, "docx");

            var newVersion = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                VersionNumber = nextVersionNumber,
                FileName = fileName,
                FilePath = relativePath,
                ContentType = FileStorage.DocxContentType,
                SizeBytes = size,
                Sha256 = sha,
                Source = DocumentSource.Generated,
                CreatedAt = DateTimeOffset.UtcNow
            };

            document.TemplateId = template.Id;
            document.Versions.Add(newVersion);
            document.CurrentVersionId = newVersion.Id;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            db.DocumentVersions.Add(newVersion);
            await db.SaveChangesAsync();

            if (result.Warnings.Count > 0)
            {
                logger.LogWarning("Generated document {DocumentId} v{Version} with warnings: {Warnings}", document.Id, nextVersionNumber, string.Join("; ", result.Warnings));
            }

            var reloaded = await LoadDocument(db, id, current.StudentId);
            return Results.Ok(new GenerateDocumentResponse(DocumentDtoMapper.ToDetail(reloaded!), result.Warnings.ToList()));
        })
        .WithName("GenerateDocument");

        group.MapPost("/{id:guid}/versions", async (Guid id, HttpRequest httpRequest, AppDbContext db, CurrentStudentProvider current,
            FileStorage storage, DocumentStatusPolicy statusPolicy, ILogger<Program> logger) =>
        {
            if (!httpRequest.HasFormContentType)
            {
                return Results.BadRequest(new { detail = "Ожидается multipart/form-data с полем file." });
            }

            var form = await httpRequest.ReadFormAsync();
            var file = form.Files["file"];
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { detail = "Файл не передан." });
            }

            if (file.Length > FileStorage.MaxSizeBytes)
            {
                return Results.Problem(detail: "Размер файла превышает 10 МБ.", statusCode: StatusCodes.Status413PayloadTooLarge);
            }

            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                content = ms.ToArray();
            }

            string extension, contentType;
            try
            {
                (extension, contentType) = FileStorage.ValidateDocumentBytes(content, file.FileName);
            }
            catch (FileValidationException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status415UnsupportedMediaType);
            }

            document.Status = statusPolicy.Apply(document.Status, DocumentTransitionTrigger.Upload);

            var nextVersionNumber = document.Versions.Count == 0 ? 1 : document.Versions.Max(v => v.VersionNumber) + 1;
            var (relativePath, size, sha) = storage.SaveVersion(document.Id, nextVersionNumber, content, extension);

            var newVersion = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                VersionNumber = nextVersionNumber,
                FileName = FileStorage.SanitizeFileName(file.FileName),
                FilePath = relativePath,
                ContentType = contentType,
                SizeBytes = size,
                Sha256 = sha,
                Source = DocumentSource.Uploaded,
                CreatedAt = DateTimeOffset.UtcNow
            };

            document.Versions.Add(newVersion);
            document.CurrentVersionId = newVersion.Id;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            db.DocumentVersions.Add(newVersion);
            await db.SaveChangesAsync();

            logger.LogInformation("Uploaded new version {Version} for document {DocumentId}", nextVersionNumber, document.Id);

            var reloaded = await LoadDocument(db, id, current.StudentId);
            return Results.Ok(DocumentDtoMapper.ToDetail(reloaded!));
        })
        .WithName("UploadDocumentVersion")
        .DisableAntiforgery();

        group.MapPut("/{id:guid}/content", async (Guid id, SaveContentRequest request, AppDbContext db, CurrentStudentProvider current) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            if (document.DocumentType!.Code != "report")
            {
                return Results.Problem(detail: "Текст содержимого можно сохранять только для отчёта по практике.", statusCode: StatusCodes.Status409Conflict);
            }

            if (document.Status is not (DocumentStatus.Draft or DocumentStatus.Returned or DocumentStatus.NotUploaded))
            {
                return Results.Problem(detail: "Содержимое можно редактировать только в статусах «Черновик», «Возвращён» или «Не загружен».", statusCode: StatusCodes.Status409Conflict);
            }

            document.Content = request.Content;
            if (document.Status == DocumentStatus.NotUploaded)
            {
                document.Status = DocumentStatus.Draft;
            }
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            var reloaded = await LoadDocument(db, id, current.StudentId);
            return Results.Ok(DocumentDtoMapper.ToDetail(reloaded!));
        })
        .WithName("SaveDocumentContent");

        group.MapPost("/{id:guid}/submit", async (Guid id, AppDbContext db, CurrentStudentProvider current, DocumentStatusPolicy statusPolicy) =>
        {
            var document = await LoadDocument(db, id, current.StudentId);
            if (document == null)
            {
                return Results.NotFound(new { detail = "Документ не найден." });
            }

            if (document.CurrentVersionId == null)
            {
                return Results.Problem(detail: "Нельзя отправить документ без загруженной версии файла.", statusCode: StatusCodes.Status409Conflict);
            }

            document.Status = statusPolicy.Apply(document.Status, DocumentTransitionTrigger.Submit);
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            var reloaded = await LoadDocument(db, id, current.StudentId);
            return Results.Ok(DocumentDtoMapper.ToDetail(reloaded!));
        })
        .WithName("SubmitDocument");

        app.MapGet("/api/document-templates", async (Guid? participantId, AppDbContext db, FileStorage storage) =>
        {
            var templates = await db.DocumentTemplates
                .Include(t => t.DocumentType)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Code)
                .ThenBy(t => t.Version)
                .ToListAsync();

            return Results.Ok(templates.Select(t => new DocumentTemplateDto(
                t.Id, t.Code, t.Name, t.DocumentType!.Code, t.ProfessionalModule, t.Version,
                t.FileName, TemplateSize(storage, t.FilePath), $"/api/document-templates/{t.Id}/download")).ToList());
        })
        .WithName("GetDocumentTemplates")
        .WithTags("Documents");

        app.MapGet("/api/document-templates/{id:guid}/download", async (Guid id, AppDbContext db, FileStorage storage) =>
        {
            var template = await db.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
            if (template == null)
            {
                return Results.NotFound(new { detail = "Шаблон документа не найден." });
            }

            if (!storage.TryResolveTemplatePath(template.FilePath, out var path) || !File.Exists(path))
            {
                return Results.NotFound(new { detail = "Файл шаблона не найден." });
            }

            var bytes = await File.ReadAllBytesAsync(path);
            return ResultsWithFileName(bytes, FileStorage.DocxContentType, template.FileName, inline: false);
        })
        .WithName("DownloadDocumentTemplate")
        .WithTags("Documents");
    }

    private static Task<Document?> LoadDocument(AppDbContext db, Guid id, Guid studentId) =>
        DocumentQueries.LoadVisibleAsync(db, id, studentId);

    private static long TemplateSize(FileStorage storage, string relativePath)
    {
        try
        {
            return storage.TryResolveTemplatePath(relativePath, out var path) && File.Exists(path)
                ? new FileInfo(path).Length
                : 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return 0;
        }
    }

    private static async Task<Domain.Entities.DocumentTemplate?> ResolveActiveTemplate(AppDbContext db, Document document)
    {
        var participant = document.Participant!;
        var query = db.DocumentTemplates.Where(t => t.IsActive && t.DocumentTypeId == document.DocumentTypeId);

        if (document.DocumentType!.Code == "report")
        {
            var byModule = await query.FirstOrDefaultAsync(t => t.ProfessionalModule == participant.Practice!.ProfessionalModule);
            if (byModule != null)
            {
                return byModule;
            }
        }

        return await query.FirstOrDefaultAsync();
    }

    private static DocumentVersion? ResolveVersion(Document document, int? versionNumber)
    {
        if (versionNumber.HasValue)
        {
            return document.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber.Value);
        }
        return document.CurrentVersion ?? document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
    }

    private static IResult ResultsWithFileName(byte[] bytes, string contentType, string fileName, bool inline)
    {
        // RFC 5987 encoding handles non-ASCII (Cyrillic) file names; FileNameStar is enough for
        // modern browsers and does not require an ASCII FileName fallback for this prototype.
        var disposition = DownloadResponseBuilder.ContentDisposition(fileName, inline);
        return new DocxFileResult(bytes, contentType, disposition);
    }

    /// <summary>
    /// Minimal IResult that writes raw bytes with an explicit Content-Disposition header,
    /// supporting RFC 5987 encoded (non-ASCII) file names for inline preview and downloads.
    /// </summary>
    private sealed class DocxFileResult(byte[] bytes, string contentType, string contentDisposition) : IResult
    {
        public System.Threading.Tasks.Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.ContentType = contentType;
            httpContext.Response.Headers.ContentDisposition = contentDisposition;
            httpContext.Response.ContentLength = bytes.Length;
            return httpContext.Response.Body.WriteAsync(bytes).AsTask();
        }
    }

    private static string SlugifyStudent(Student student)
    {
        return student.StudentNumber.Replace(" ", "-").ToLowerInvariant();
    }
}
