using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticeManager.Api.Data;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Domain.Enums;
using PracticeManager.Api.Features.Documents;
using PracticeManager.Api.Services;
using PracticeManager.Api.Services.DocumentGeneration;
using Task = System.Threading.Tasks.Task;

namespace PracticeManager.Tests;

/// <summary>
/// Invokes the real minimal-API DELETE delegate against the local database. Temporary documents are
/// created for each case, so seeded rows are never modified.
/// </summary>
[Collection("PostgreSQL")]
public class DocumentDeleteEndpointTests
{
    private static readonly Guid StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid RevisionParticipantId = Guid.Parse("00000000-0000-0000-0000-000000000020");

    [Fact]
    public async Task Delete_ReturnedDocument_Returns204SoftDeletesAndKeepsVersion()
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var app = BuildApp();
        var documentId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
        var versionId = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

        await CreateTemporaryDocumentAsync(documentId, versionId, DocumentStatus.Returned);
        try
        {
            var response = await InvokeDeleteAsync(app, documentId);

            Assert.Equal(StatusCodes.Status204NoContent, response.Response.StatusCode);
            await using var db = new AppDbContext(TestDatabase.Options);
            Assert.NotNull(await db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId && d.DeletedAt != null));
            Assert.True(await db.DocumentVersions.AnyAsync(v => v.Id == versionId && v.DocumentId == documentId));
            Assert.Null(await DocumentQueries.LoadVisibleAsync(db, documentId, StudentId));

            var secondResponse = await InvokeDeleteAsync(app, documentId);
            Assert.Equal(StatusCodes.Status404NotFound, secondResponse.Response.StatusCode);
        }
        finally
        {
            await RemoveTemporaryDocumentAsync(documentId, versionId);
        }
    }

    [Theory]
    [InlineData(DocumentStatus.Submitted)]
    [InlineData(DocumentStatus.Accepted)]
    [InlineData(DocumentStatus.Draft)]
    [InlineData(DocumentStatus.NotUploaded)]
    public async Task Delete_NonReturnedDocument_ReturnsRussianConflict(DocumentStatus status)
    {
        if (!await TestDatabase.IsReadyAsync())
        {
            return;
        }

        await using var app = BuildApp();
        var suffix = (int)status + 10;
        var documentId = new Guid($"cccccccc-0000-0000-0000-{suffix:000000000001}");
        var versionId = new Guid($"cccccccc-0000-0000-0000-{suffix:000000000002}");

        await CreateTemporaryDocumentAsync(documentId, versionId, status);
        try
        {
            var response = await InvokeDeleteAsync(app, documentId);

            Assert.Equal(StatusCodes.Status409Conflict, response.Response.StatusCode);
            response.Response.Body.Position = 0;
            var body = await new StreamReader(response.Response.Body, Encoding.UTF8).ReadToEndAsync();
            Assert.Contains("удалён", body, StringComparison.OrdinalIgnoreCase);

            await using var db = new AppDbContext(TestDatabase.Options);
            Assert.Null((await db.Documents.AsNoTracking().FirstAsync(d => d.Id == documentId)).DeletedAt);
        }
        finally
        {
            await RemoveTemporaryDocumentAsync(documentId, versionId);
        }
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddProblemDetails();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(TestDatabase.ConnectionString));
        builder.Services.AddScoped<CurrentStudentProvider>();
        builder.Services.AddSingleton<FileStorage>();
        builder.Services.AddSingleton<DocxTemplateRenderer>();
        builder.Services.AddSingleton<DocumentStatusPolicy>();

        var app = builder.Build();
        app.MapDocumentsEndpoints();
        return app;
    }

    private static async Task<DefaultHttpContext> InvokeDeleteAsync(WebApplication app, Guid documentId)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .First(endpoint => endpoint.RoutePattern.RawText == "/api/documents/{id:guid}"
                && endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains("DELETE") == true);

        await using var scope = app.Services.CreateAsyncScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        httpContext.Request.Method = "DELETE";
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = $"/api/documents/{documentId}";
        httpContext.Request.RouteValues = new RouteValueDictionary { ["id"] = documentId.ToString() };
        httpContext.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(httpContext);
        return httpContext;
    }

    private static async Task CreateTemporaryDocumentAsync(Guid documentId, Guid versionId, DocumentStatus status)
    {
        await using var db = new AppDbContext(TestDatabase.Options);
        var participant = await db.PracticeParticipants.FirstAsync(p => p.Id == RevisionParticipantId);
        var typeId = await db.DocumentTypes.Where(t => t.Code == "characteristic").Select(t => t.Id).FirstAsync();
        var now = DateTimeOffset.UtcNow;

        var version = new DocumentVersion
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
            CreatedAt = now
        };
        db.DocumentVersions.Add(version);
        db.Documents.Add(new Document
        {
            Id = documentId,
            ParticipantId = participant.Id,
            DocumentTypeId = typeId,
            Name = "DELETE endpoint test",
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    private static async Task RemoveTemporaryDocumentAsync(Guid documentId, Guid versionId)
    {
        await using var db = new AppDbContext(TestDatabase.Options);
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId);
        if (document != null)
        {
            db.Documents.Remove(document);
        }
        var version = await db.DocumentVersions.FirstOrDefaultAsync(v => v.Id == versionId);
        if (version != null)
        {
            db.DocumentVersions.Remove(version);
        }
        await db.SaveChangesAsync();
    }
}
