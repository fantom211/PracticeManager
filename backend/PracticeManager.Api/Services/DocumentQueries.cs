using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Data;
using PracticeManager.Api.Domain.Entities;

namespace PracticeManager.Api.Services;

/// <summary>
/// Single place for the soft-delete predicates used by every student-facing document read.
/// </summary>
public static class DocumentQueries
{
    public static IQueryable<Document> VisibleForStudent(AppDbContext db, Guid studentId, Guid? participantId = null)
    {
        var query = db.Documents
            .Where(d => d.DeletedAt == null && d.Participant!.StudentId == studentId);

        if (participantId.HasValue)
        {
            query = query.Where(d => d.ParticipantId == participantId.Value);
        }

        return query;
    }

    public static async Task<Document?> LoadVisibleAsync(AppDbContext db, Guid id, Guid studentId)
    {
        return await VisibleForStudent(db, studentId)
            .Include(d => d.DocumentType)
            .Include(d => d.Versions)
            .Include(d => d.Comments)
            .Include(d => d.CurrentVersion)
            .Include(d => d.Participant).ThenInclude(p => p!.Student).ThenInclude(s => s!.Group).ThenInclude(g => g!.Specialty)
            .Include(d => d.Participant).ThenInclude(p => p!.Practice).ThenInclude(pr => pr!.Teacher)
            .Include(d => d.Participant).ThenInclude(p => p!.Practice).ThenInclude(pr => pr!.PracticeType)
            .Include(d => d.Participant).ThenInclude(p => p!.Organization)
            .Include(d => d.Participant).ThenInclude(p => p!.Assessment)
            .Include(d => d.Participant).ThenInclude(p => p!.DiaryEntries)
            .Include(d => d.Participant).ThenInclude(p => p!.Tasks)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
