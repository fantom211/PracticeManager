using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Contracts;
using PracticeManager.Api.Data;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Services;

namespace PracticeManager.Api.Features.Notifications;

public static class NotificationsEndpoints
{
    public static void MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        group.MapGet("/", async (AppDbContext db, CurrentStudentProvider current) =>
        {
            var notifications = await db.Notifications
                .Where(n => n.StudentId == current.StudentId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Results.Ok(await ToDtosAsync(db, notifications));
        })
        .WithName("GetNotifications");

        group.MapPost("/{id:guid}/read", async (Guid id, AppDbContext db, CurrentStudentProvider current) =>
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.StudentId == current.StudentId);
            if (notification == null)
            {
                return Results.NotFound(new { detail = "Уведомление не найдено." });
            }

            notification.IsRead = true;
            await db.SaveChangesAsync();
            var dto = (await ToDtosAsync(db, [notification]))[0];
            return Results.Ok(dto);
        })
        .WithName("MarkNotificationRead");

        group.MapPost("/read-all", async (AppDbContext db, CurrentStudentProvider current) =>
        {
            var notifications = await db.Notifications
                .Where(n => n.StudentId == current.StudentId && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { updated = notifications.Count });
        })
        .WithName("MarkAllNotificationsRead");
    }

    private static async Task<List<NotificationDto>> ToDtosAsync(AppDbContext db, List<Notification> notifications)
    {
        var linkedIds = NotificationLinkResolver.ParseDocumentIds(notifications.Select(n => n.Link));
        var visibleIds = linkedIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.Documents
                .Where(d => linkedIds.Contains(d.Id) && d.DeletedAt == null)
                .Select(d => d.Id)
                .ToListAsync()).ToHashSet();

        return notifications
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message, n.IsRead,
                NotificationLinkResolver.Resolve(n.Link, visibleIds), n.CreatedAt))
            .ToList();
    }
}
