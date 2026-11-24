using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Data;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Domain.Enums;
using PracticeManager.Api.Services;
using Task = System.Threading.Tasks.Task;

namespace PracticeManager.Tests;

/// <summary>
/// Optional integration coverage against the local PostgreSQL instance. Every test returns early
/// when the database or the revision migration is unavailable.
/// </summary>
[Collection("PostgreSQL")]
public class RevisionSeedIntegrationTests
{
    private static readonly Guid StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid RevisionParticipantId = Guid.Parse("00000000-0000-0000-0000-000000000020");

    [Fact]
    public async Task AdditionalProductionPractice_IsSeededWithOwnData()
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var db = new AppDbContext(TestDatabase.Options);
        var participant = await db.PracticeParticipants
            .Include(p => p.Practice)
            .Include(p => p.Assessment)
            .FirstOrDefaultAsync(p => p.Id == RevisionParticipantId);

        Assert.NotNull(participant);
        Assert.Equal(StudentId, participant.StudentId);
        Assert.Equal(ParticipantStatus.Completed, participant.Status);
        Assert.Equal(100, participant.ProgressPercent);
        Assert.Equal("4 (хорошо)", participant.Assessment?.Grade);
        Assert.Contains("ПМ.03", participant.Practice!.ProfessionalModule, StringComparison.Ordinal);
        Assert.Equal("ООО «Гамма»", (await db.Organizations.FirstAsync(o => o.Id == participant.OrganizationId)).Name);
        Assert.Equal(4, await db.Documents.CountAsync(d => d.ParticipantId == participant.Id));
        Assert.Equal(3, await db.Tasks.CountAsync(t => t.ParticipantId == participant.Id));
        Assert.Equal(4, await db.DiaryEntries.CountAsync(e => e.ParticipantId == participant.Id));
        Assert.Equal(2, await db.Notifications.CountAsync(n => n.Title.Contains("ПМ.03")));
    }

    [Fact]
    public async Task TeacherComments_AreSeededNewestFirst()
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var db = new AppDbContext(TestDatabase.Options);
        var comments = await db.TeacherComments
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        Assert.True(comments.Count >= 5);
        Assert.Equal("00000000-0000-0000-0000-400000000005", comments[0].Id.ToString());
        Assert.Contains("report-pm03.docx", comments[0].FullText, StringComparison.Ordinal);
        Assert.Equal("Петрова Мария Ильинична", comments[0].AuthorName);
        Assert.Contains("1.", comments[1].FullText, StringComparison.Ordinal);
        Assert.Contains("интеграцию модуля авторизации", comments[^1].FullText, StringComparison.Ordinal);
        Assert.True(comments.All(c => c.AuthorRole == "Преподаватель"));
        Assert.True(comments.All(c => c.FullText.Contains('\n')));
    }

    [Fact]
    public async Task SoftDeletedDocument_IsHiddenFromReadsButKeepsVersions()
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var db = new AppDbContext(TestDatabase.Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var participant = await db.PracticeParticipants.FirstAsync(p => p.Id == RevisionParticipantId);
            var typeId = await db.DocumentTypes.Where(t => t.Code == "characteristic").Select(t => t.Id).FirstAsync();
            var documentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
            var versionId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

            db.Documents.Add(new Document
            {
                Id = documentId,
                ParticipantId = participant.Id,
                DocumentTypeId = typeId,
                Name = "Soft delete test document",
                Status = DocumentStatus.Returned,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.DocumentVersions.Add(new DocumentVersion
            {
                Id = versionId,
                DocumentId = documentId,
                VersionNumber = 1,
                FileName = "v1.docx",
                FilePath = $"{documentId}/v1.docx",
                ContentType = FileStorage.DocxContentType,
                SizeBytes = 12,
                Sha256 = new string('a', 64),
                Source = DocumentSource.Seed,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var policy = new DocumentStatusPolicy();
            policy.EnsureCanDelete(DocumentStatus.Returned);
            var document = await db.Documents.FirstAsync(d => d.Id == documentId);
            document.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            Assert.Null(await DocumentQueries.LoadVisibleAsync(db, documentId, StudentId));
            Assert.False(await DocumentQueries.VisibleForStudent(db, StudentId).AnyAsync(d => d.Id == documentId));
            Assert.True(await db.DocumentVersions.AnyAsync(v => v.DocumentId == documentId && v.VersionNumber == 1));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task ParticipantScoping_FiltersDocumentsByParticipant()
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var db = new AppDbContext(TestDatabase.Options);
        var originalParticipantId = await db.PracticeParticipants
            .Where(p => p.StudentId == StudentId && p.Practice!.ProfessionalModule.StartsWith("ПМ.02"))
            .Select(p => p.Id)
            .FirstAsync();

        var revisionDocuments = await DocumentQueries.VisibleForStudent(db, StudentId, RevisionParticipantId).ToListAsync();
        var originalDocuments = await DocumentQueries.VisibleForStudent(db, StudentId, originalParticipantId).ToListAsync();

        Assert.NotEmpty(revisionDocuments);
        Assert.NotEmpty(originalDocuments);
        Assert.All(revisionDocuments, d => Assert.Equal(RevisionParticipantId, d.ParticipantId));
        Assert.All(originalDocuments, d => Assert.Equal(originalParticipantId, d.ParticipantId));
        Assert.Equal(4, revisionDocuments.Count);
        Assert.Equal(4, originalDocuments.Count);
    }

    [Fact]
    public async Task ActiveTemplates_IncludeDownloadablePm03Report()
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var db = new AppDbContext(TestDatabase.Options);
        var templates = await db.DocumentTemplates.Where(t => t.IsActive).ToListAsync();

        Assert.Equal(6, templates.Count);
        var pm03 = Assert.Single(templates, t => t.Code == "report-pm03");
        Assert.Equal("report-pm03.docx", pm03.FileName);
        Assert.Equal("report-pm03.docx", pm03.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(pm03.Sha256));
    }
}
