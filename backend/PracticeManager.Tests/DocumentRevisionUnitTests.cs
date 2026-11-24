using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using PracticeManager.Api.Contracts;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Domain.Enums;
using PracticeManager.Api.Services;

namespace PracticeManager.Tests;

public class DocumentRevisionUnitTests
{
    private readonly DocumentStatusPolicy _policy = new();

    [Theory]
    [InlineData(DocumentStatus.Returned, true)]
    [InlineData(DocumentStatus.Submitted, false)]
    [InlineData(DocumentStatus.Accepted, false)]
    [InlineData(DocumentStatus.Draft, false)]
    [InlineData(DocumentStatus.NotUploaded, false)]
    public void CanDelete_OnlyReturned(DocumentStatus status, bool expected)
    {
        Assert.Equal(expected, _policy.CanDelete(status));
    }

    [Fact]
    public void EnsureCanDelete_Returned_DoesNotThrow()
    {
        _policy.EnsureCanDelete(DocumentStatus.Returned);
    }

    [Theory]
    [InlineData(DocumentStatus.Submitted)]
    [InlineData(DocumentStatus.Accepted)]
    [InlineData(DocumentStatus.Draft)]
    [InlineData(DocumentStatus.NotUploaded)]
    public void EnsureCanDelete_OtherStatuses_ThrowRussianConflict(DocumentStatus status)
    {
        var ex = Assert.Throws<DocumentStatusConflictException>(() => _policy.EnsureCanDelete(status));
        Assert.Equal(409, ex.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Contains("удалён", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToListItem_Returned_CanDeleteAndUsesNewestTeacherCommentSummary()
    {
        var document = MakeDocument(DocumentStatus.Returned);
        document.Comments.Add(MakeComment(Guid.Parse("00000000-0000-0000-0000-400000000001"),
            "старый комментарий", SeedTime("2027-03-09T10:00:00+03:00")));
        document.Comments.Add(MakeComment(Guid.Parse("00000000-0000-0000-0000-400000000002"),
            "Необходимо добавить информацию о задачах", SeedTime("2027-03-09T12:18:00+03:00")));

        var dto = DocumentDtoMapper.ToListItem(document);

        Assert.True(dto.CanDelete);
        Assert.Equal("Необходимо добавить информацию о задачах", dto.TeacherComment);
    }

    [Theory]
    [InlineData(DocumentStatus.Submitted)]
    [InlineData(DocumentStatus.Accepted)]
    [InlineData(DocumentStatus.Draft)]
    [InlineData(DocumentStatus.NotUploaded)]
    public void ToListItem_NonReturned_CannotDelete(DocumentStatus status)
    {
        var dto = DocumentDtoMapper.ToListItem(MakeDocument(status));
        Assert.False(dto.CanDelete);
    }

    [Fact]
    public void ToDetail_SoftDeletedDocument_HasCanDeleteFalse()
    {
        var document = MakeDocument(DocumentStatus.Returned, SeedTime("2027-03-09T13:00:00+03:00"));
        var dto = DocumentDtoMapper.ToDetail(document);
        Assert.False(dto.CanDelete);
    }

    [Fact]
    public void ToDetail_AddsCommentsNewestFirstAndFileUrls()
    {
        var document = MakeDocument(DocumentStatus.Returned);
        var older = MakeComment(Guid.Parse("00000000-0000-0000-0000-400000000001"),
            "Первое замечание", SeedTime("2027-03-09T10:00:00+03:00"));
        var newer = MakeComment(Guid.Parse("00000000-0000-0000-0000-400000000002"),
            "Второе замечание", SeedTime("2027-03-10T11:00:00+03:00"));
        document.Comments.AddRange([older, newer]);

        var dto = DocumentDtoMapper.ToDetail(document);

        Assert.Equal(2, dto.Comments.Count);
        Assert.Equal(newer.Id, dto.Comments[0].Id);
        Assert.Equal(older.Id, dto.Comments[1].Id);
        Assert.Equal(newer.Summary, dto.TeacherComment);
        Assert.Equal($"/api/documents/{document.Id}/preview", dto.PreviewUrl);
        Assert.Equal($"/api/documents/{document.Id}/download", dto.DownloadUrl);
        Assert.Equal($"/api/documents/{document.Id}/download?version=1", Assert.Single(dto.Versions).DownloadUrl);
    }

    [Fact]
    public void ResolveTeacherComment_FallsBackToLegacyVersionComment()
    {
        var document = MakeDocument(DocumentStatus.Accepted);
        document.CurrentVersion!.TeacherComment = " legacy summary ";
        Assert.Equal(" legacy summary ", DocumentDtoMapper.ResolveTeacherComment(document));
    }

    [Fact]
    public void TemplateDownload_UsesDocxContentTypeAndRfc5987FileName()
    {
        const string fileName = "Отчёт по практике (ПМ.02).docx";
        var disposition = DownloadResponseBuilder.ContentDisposition(fileName, inline: false);

        Assert.StartsWith("attachment", disposition, StringComparison.Ordinal);
        Assert.Contains("filename*=UTF-8''", disposition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("%D0%9E", disposition, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileStorage.DocxContentType);
        Assert.StartsWith("inline", DownloadResponseBuilder.ContentDisposition(fileName, inline: true), StringComparison.Ordinal);
    }

    [Fact]
    public void TemplatePathResolver_RejectsTraversalAndAbsolutePaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "pm-templates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllBytes(Path.Combine(root, "report-pm02.docx"), [1, 2, 3]);
            var storage = CreateStorage(root);

            Assert.True(storage.TryResolveTemplatePath("report-pm02.docx", out var resolved));
            Assert.Equal(Path.Combine(root, "report-pm02.docx"), resolved);
            Assert.True(File.Exists(resolved));
            Assert.False(storage.TryResolveTemplatePath("..\\outside.docx", out _));
            Assert.False(storage.TryResolveTemplatePath("../outside.docx", out _));
            Assert.False(storage.TryResolveTemplatePath(Path.Combine(root, "report-pm02.docx"), out _));
            Assert.Throws<FileValidationException>(() => storage.ResolveTemplatePath("../outside.docx"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void NotificationLinks_ToDeletedDocuments_FallBackToList()
    {
        var deletedId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var visibleId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var links = new[]
        {
            $"/documents/{deletedId}",
            $"/documents/{visibleId}?version=1",
            "/documents",
            "/tasks"
        };

        var ids = NotificationLinkResolver.ParseDocumentIds(links);
        Assert.Equal(2, ids.Count);
        Assert.Contains(deletedId, ids);
        Assert.Contains(visibleId, ids);

        var visible = new HashSet<Guid> { visibleId };
        Assert.Equal("/documents", NotificationLinkResolver.Resolve(links[0], visible));
        Assert.Equal(links[1], NotificationLinkResolver.Resolve(links[1], visible));
        Assert.Equal("/documents", NotificationLinkResolver.Resolve(links[2], visible));
        Assert.Equal("/tasks", NotificationLinkResolver.Resolve(links[3], visible));
    }

    private static FileStorage CreateStorage(string templatesRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Templates:Root"] = templatesRoot,
                ["Storage:Root"] = Path.Combine(Path.GetTempPath(), "pm-storage-" + Guid.NewGuid().ToString("N"))
            })
            .Build();
        return new FileStorage(configuration, new FakeEnvironment(), NullLogger<FileStorage>.Instance);
    }

    private static Document MakeDocument(DocumentStatus status, DateTimeOffset? deletedAt = null)
    {
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.Empty,
            VersionNumber = 1,
            FileName = "v1.docx",
            FilePath = "v1.docx",
            ContentType = FileStorage.DocxContentType,
            SizeBytes = 100,
            Sha256 = new string('a', 64),
            Source = DocumentSource.Seed,
            CreatedAt = SeedTime("2027-03-09T12:15:00+03:00")
        };
        return new Document
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Name = "Характеристика от организации",
            DocumentType = new DocumentType { Code = "characteristic", Name = "Характеристика от организации" },
            Status = status,
            UpdatedAt = SeedTime("2027-03-09T12:15:00+03:00"),
            DeletedAt = deletedAt,
            CurrentVersion = version,
            Versions = [version]
        };
    }

    private static TeacherComment MakeComment(Guid id, string summary, DateTimeOffset createdAt) => new()
    {
        Id = id,
        DocumentId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
        VersionNumber = 1,
        AuthorName = "Иванов Петр Петрович",
        AuthorRole = "Преподаватель",
        Kind = "return",
        Summary = summary,
        FullText = summary + "\n\n1. Подробности",
        CreatedAt = createdAt
    };

    private static DateTimeOffset SeedTime(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "PracticeManager.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public bool IsDevelopment() => true;
        public bool IsProduction() => false;
        public bool IsStaging() => false;
    }
}
