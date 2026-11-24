using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Contracts;
using PracticeManager.Api.Data;
using PracticeManager.Api.Services;

namespace PracticeManager.Api.Features.Profile;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (AppDbContext db, CurrentStudentProvider current) =>
        {
            var student = await db.Students
                .Include(s => s.Group).ThenInclude(g => g!.Specialty)
                .FirstOrDefaultAsync(s => s.Id == current.StudentId);

            if (student == null)
            {
                return Results.NotFound();
            }

            var response = new MeResponse(
                student.Id,
                student.FullName,
                student.ShortName,
                student.Group!.Name,
                student.Group.Specialty!.Code,
                student.Group.Specialty.Name,
                CalculateCourse(student.Group.AdmissionYear),
                student.StudentNumber,
                student.Phone,
                student.Email);

            return Results.Ok(response);
        })
        .WithName("GetMe")
        .WithTags("Profile");
    }

    private static int CalculateCourse(int admissionYear)
    {
        var now = DateTime.UtcNow;
        var academicYear = now.Month >= 9 ? now.Year : now.Year - 1;
        return Math.Max(1, academicYear - admissionYear + 1);
    }
}
