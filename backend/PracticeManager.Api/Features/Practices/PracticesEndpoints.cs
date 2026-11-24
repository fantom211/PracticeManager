using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Contracts;
using PracticeManager.Api.Data;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Services;

namespace PracticeManager.Api.Features.Practices;

public static class PracticesEndpoints
{
    public static void MapPracticesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/practices").WithTags("Practices");

        group.MapGet("/", async (AppDbContext db, CurrentStudentProvider current) =>
        {
            var participants = await LoadParticipantsQuery(db, current.StudentId).ToListAsync();
            var currentParticipant = participants.FirstOrDefault(p => p.Status == Domain.Enums.ParticipantStatus.InProgress
                || p.Status == Domain.Enums.ParticipantStatus.NotStarted
                || p.Status == Domain.Enums.ParticipantStatus.Assigned);

            var unread = await db.Notifications.CountAsync(n => n.StudentId == current.StudentId && !n.IsRead);

            var currentDto = currentParticipant == null ? null : await BuildCurrentResponse(db, currentParticipant, unread);
            var history = participants
                .Where(p => currentParticipant == null || p.Id != currentParticipant.Id)
                .OrderByDescending(p => p.Practice!.EndDate)
                .Select(BuildHistoryItem)
                .ToList();

            return Results.Ok(new PracticesResponse(currentDto, history));
        })
        .WithName("GetPractices");

        group.MapGet("/current", async (AppDbContext db, CurrentStudentProvider current) =>
        {
            var participants = await LoadParticipantsQuery(db, current.StudentId).ToListAsync();
            var currentParticipant = participants.FirstOrDefault(p => p.Status == Domain.Enums.ParticipantStatus.InProgress
                || p.Status == Domain.Enums.ParticipantStatus.NotStarted
                || p.Status == Domain.Enums.ParticipantStatus.Assigned);

            if (currentParticipant == null)
            {
                return Results.NotFound(new { detail = "Текущая практика не найдена." });
            }

            var unread = await db.Notifications.CountAsync(n => n.StudentId == current.StudentId && !n.IsRead);
            return Results.Ok(await BuildCurrentResponse(db, currentParticipant, unread));
        })
        .WithName("GetCurrentPractice");

        group.MapGet("/{participantId:guid}", async (Guid participantId, AppDbContext db, CurrentStudentProvider current) =>
        {
            var participant = await LoadParticipantsQuery(db, current.StudentId)
                .FirstOrDefaultAsync(p => p.Id == participantId);

            if (participant == null)
            {
                return Results.NotFound(new { detail = "Практика не найдена." });
            }

            var unread = await db.Notifications.CountAsync(n => n.StudentId == current.StudentId && !n.IsRead);
            return Results.Ok(await BuildCurrentResponse(db, participant, unread));
        })
        .WithName("GetPracticeDetail");

        group.MapGet("/{participantId:guid}/tasks", async (Guid participantId, AppDbContext db, CurrentStudentProvider current) =>
        {
            var exists = await ParticipantBelongsToStudent(db, participantId, current.StudentId);
            if (!exists)
            {
                return Results.NotFound(new { detail = "Практика не найдена." });
            }

            var tasks = await db.Tasks
                .Where(t => t.ParticipantId == participantId)
                .OrderBy(t => t.Deadline)
                .Select(t => new TaskItem(t.Id, t.Title, t.Description, t.Deadline,
                    StatusLabels.CamelCase(t.Status), StatusLabels.Task(t.Status), t.TeacherComment, t.IsGroup))
                .ToListAsync();

            return Results.Ok(tasks);
        })
        .WithName("GetPracticeTasks");

        group.MapGet("/{participantId:guid}/diary", async (Guid participantId, AppDbContext db, CurrentStudentProvider current) =>
        {
            var exists = await ParticipantBelongsToStudent(db, participantId, current.StudentId);
            if (!exists)
            {
                return Results.NotFound(new { detail = "Практика не найдена." });
            }

            var entries = await db.DiaryEntries
                .Where(d => d.ParticipantId == participantId)
                .OrderBy(d => d.SortOrder)
                .Select(d => new DiaryEntryDto(d.Id, d.Date, d.WorkDescription, d.SupervisorMark))
                .ToListAsync();

            return Results.Ok(entries);
        })
        .WithName("GetPracticeDiary");

        group.MapPut("/{participantId:guid}/diary", async (Guid participantId, UpdateDiaryRequest request, AppDbContext db, CurrentStudentProvider current) =>
        {
            var exists = await ParticipantBelongsToStudent(db, participantId, current.StudentId);
            if (!exists)
            {
                return Results.NotFound(new { detail = "Практика не найдена." });
            }

            var existing = db.DiaryEntries.Where(d => d.ParticipantId == participantId);
            db.DiaryEntries.RemoveRange(existing);

            var sortOrder = 0;
            foreach (var entry in request.Entries.OrderBy(e => e.Date))
            {
                db.DiaryEntries.Add(new DiaryEntry
                {
                    Id = Guid.NewGuid(),
                    ParticipantId = participantId,
                    Date = entry.Date,
                    WorkDescription = entry.WorkDescription,
                    SupervisorMark = entry.SupervisorMark,
                    SortOrder = sortOrder++
                });
            }

            await db.SaveChangesAsync();

            var updated = await db.DiaryEntries
                .Where(d => d.ParticipantId == participantId)
                .OrderBy(d => d.SortOrder)
                .Select(d => new DiaryEntryDto(d.Id, d.Date, d.WorkDescription, d.SupervisorMark))
                .ToListAsync();

            return Results.Ok(updated);
        })
        .WithName("UpdatePracticeDiary");
    }

    private static IQueryable<PracticeParticipant> LoadParticipantsQuery(AppDbContext db, Guid studentId)
    {
        return db.PracticeParticipants
            .Where(p => p.StudentId == studentId)
            .Include(p => p.Practice).ThenInclude(pr => pr!.PracticeType)
            .Include(p => p.Practice).ThenInclude(pr => pr!.Teacher)
            .Include(p => p.Practice).ThenInclude(pr => pr!.Group).ThenInclude(g => g!.Specialty)
            .Include(p => p.Practice).ThenInclude(pr => pr!.Stages)
            .Include(p => p.Organization)
            .Include(p => p.StageProgress).ThenInclude(sp => sp.Stage)
            .Include(p => p.Assessment)
            .Include(p => p.CurrentStage);
    }

    private static async Task<bool> ParticipantBelongsToStudent(AppDbContext db, Guid participantId, Guid studentId)
    {
        return await db.PracticeParticipants.AnyAsync(p => p.Id == participantId && p.StudentId == studentId);
    }

    private static async Task<PracticeCurrentResponse> BuildCurrentResponse(AppDbContext db, PracticeParticipant participant, int unreadNotifications)
    {
        var practice = participant.Practice!;

        var stages = practice.Stages
            .OrderBy(s => s.Order)
            .Select(s =>
            {
                var progress = participant.StageProgress.FirstOrDefault(sp => sp.StageId == s.Id);
                var status = progress?.Status ?? Domain.Enums.StageStatus.NotStarted;
                return new StageInfo(s.Name, StatusLabels.CamelCase(status), StatusLabels.Stage(status));
            })
            .ToList();

        var currentStage = participant.CurrentStage == null
            ? null
            : stages.FirstOrDefault(s => s.Name == participant.CurrentStage.Name);

        AssessmentInfo? assessment = participant.Assessment == null
            ? null
            : new AssessmentInfo(
                participant.Assessment.Grade,
                StatusLabels.CamelCase(participant.Assessment.Result),
                StatusLabels.Result(participant.Assessment.Result),
                participant.Assessment.AssessedAt == null ? null : DateOnly.FromDateTime(participant.Assessment.AssessedAt.Value.Date),
                participant.Assessment.Comment);

        DeadlineInfo? deadline = null;
        if (participant.Status == Domain.Enums.ParticipantStatus.InProgress || participant.Status == Domain.Enums.ParticipantStatus.NotStarted)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var daysLeft = practice.EndDate.DayNumber - today.DayNumber;
            deadline = new DeadlineInfo(practice.EndDate, "Окончание практики", daysLeft);
        }

        return new PracticeCurrentResponse(
            participant.Id,
            practice.Name,
            practice.PracticeType?.Name ?? "—",
            practice.ProfessionalModule,
            practice.Group!.Specialty!.Code,
            practice.Group.Specialty.Name,
            practice.StartDate,
            practice.EndDate,
            new OrganizationInfo(participant.Organization!.Name, participant.Organization.ActualAddress ?? participant.Organization.LegalAddress, participant.Organization.Inn),
            new PersonInfo(practice.Teacher!.ShortName, practice.Teacher.FullName, practice.Teacher.Position),
            new PersonInfo(ShortNameFromFull(participant.ExternalSupervisorName), participant.ExternalSupervisorName ?? "—", participant.ExternalSupervisorPosition),
            StatusLabels.CamelCase(participant.Status),
            StatusLabels.Participant(participant.Status),
            participant.ProgressPercent,
            currentStage,
            stages,
            assessment,
            deadline,
            unreadNotifications);
    }

    private static PracticeHistoryItem BuildHistoryItem(PracticeParticipant participant)
    {
        var practice = participant.Practice!;
        return new PracticeHistoryItem(
            participant.Id,
            practice.Name,
            practice.ProfessionalModule,
            practice.StartDate,
            practice.EndDate,
            participant.Organization?.Name ?? "—",
            StatusLabels.CamelCase(participant.Status),
            StatusLabels.Participant(participant.Status),
            participant.Assessment?.Grade,
            participant.Assessment == null ? "—" : StatusLabels.Result(participant.Assessment.Result));
    }

    private static string ShortNameFromFull(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "—";
        }
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length <= 1)
        {
            return fullName;
        }
        var last = parts[0];
        var initials = string.Join(" ", parts.Skip(1).Select(p => p[0] + "."));
        return $"{last} {initials}";
    }
}
