using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Contracts;
using PracticeManager.Api.Data;
using PracticeManager.Api.Services;

namespace PracticeManager.Api.Features.Tasks;

public static class TasksEndpoints
{
    public static void MapTasksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tasks", async (AppDbContext db, CurrentStudentProvider current) =>
        {
            var tasks = await db.Tasks
                .Where(t => t.Participant!.StudentId == current.StudentId)
                .OrderBy(t => t.Deadline)
                .Select(t => new TaskItem(t.Id, t.Title, t.Description, t.Deadline,
                    StatusLabels.CamelCase(t.Status), StatusLabels.Task(t.Status), t.TeacherComment, t.IsGroup))
                .ToListAsync();

            return Results.Ok(tasks);
        })
        .WithName("GetAllTasks")
        .WithTags("Tasks");
    }
}
