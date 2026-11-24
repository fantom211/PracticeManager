using System.Globalization;
using PracticeManager.Api.Domain.Entities;

namespace PracticeManager.Api.Services.DocumentGeneration;

public static class TemplateValueBuilder
{
    public const string CollegeName = "Московский международный колледж цифровых технологий «Академия ТОП»";
    public const string CollegeAddress = "121170, г. Москва, вн.тер.г. муниципальный округ Дорогомилово, пр-кт Кутузовский, д. 36, стр. 2";

    private static string FormatDate(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static Dictionary<string, string> Build(
        PracticeParticipant participant,
        Document document,
        List<DiaryEntry>? diaryEntries = null,
        List<Domain.Entities.Task>? tasks = null)
    {
        var practice = participant.Practice!;
        var student = participant.Student!;
        var group = student.Group!;
        var specialty = group.Specialty!;
        var org = participant.Organization!;
        var assessment = participant.Assessment;

        var values = new Dictionary<string, string>
        {
            ["college_name"] = CollegeName,
            ["college_address"] = CollegeAddress,
            ["city"] = "Калининград",
            ["year"] = practice.EndDate.Year.ToString(CultureInfo.InvariantCulture),

            ["student_full_name"] = student.FullName,
            ["student_short_name"] = student.ShortName,
            ["student_dative_full_name"] = student.FullNameDative,
            ["student_genitive_full_name"] = student.FullNameGenitive,
            ["student_group"] = group.Name,
            ["student_number"] = student.StudentNumber,
            ["student_course"] = practice.Course.ToString(CultureInfo.InvariantCulture),
            ["student_phone"] = student.Phone ?? "—",
            ["student_email"] = student.Email ?? "—",

            ["specialty_code"] = specialty.Code,
            ["specialty_name"] = specialty.Name,

            ["practice_type"] = practice.PracticeType?.Name ?? "—",
            ["practice_name"] = practice.Name,
            ["professional_module"] = practice.ProfessionalModule,
            ["practice_hours"] = practice.Hours.ToString(CultureInfo.InvariantCulture),

            ["date_start"] = FormatDate(practice.StartDate),
            ["date_end"] = FormatDate(practice.EndDate),
            ["practice_period"] = $"{FormatDate(practice.StartDate)} – {FormatDate(practice.EndDate)}",

            ["organization_name"] = org.Name,
            ["organization_address"] = org.ActualAddress ?? org.LegalAddress ?? "—",
            ["organization_inn"] = org.Inn ?? "—",

            ["organization_supervisor"] = ShortNameFromFull(participant.ExternalSupervisorName) ?? "—",
            ["organization_supervisor_full_name"] = participant.ExternalSupervisorName ?? "—",
            ["organization_supervisor_position"] = participant.ExternalSupervisorPosition ?? "—",

            ["college_supervisor"] = practice.Teacher?.ShortName ?? "—",
            ["college_supervisor_full_name"] = practice.Teacher?.FullName ?? "—",
            ["college_supervisor_position"] = practice.Teacher?.Position ?? "—",

            ["assessment_grade"] = assessment?.Grade ?? "—",
            ["assessment_result"] = assessment == null ? "—" : StatusLabels.Result(assessment.Result),
            ["assessment_date"] = assessment?.AssessedAt is { } at ? FormatDate(DateOnly.FromDateTime(at.Date)) : "—",
            ["assessment_comment"] = assessment?.Comment ?? "",

            ["report_content"] = document.DocumentType?.Code == "report" ? (document.Content ?? "") : "",
            ["characteristic_text"] = document.DocumentType?.Code == "characteristic" ? (document.Content ?? "") : "",
            ["review_text"] = document.DocumentType?.Code == "review" ? (document.Content ?? "") : "",
        };

        return values;
    }

    public static List<Dictionary<string, string>> BuildDiaryRows(List<DiaryEntry> entries)
    {
        return entries
            .OrderBy(e => e.SortOrder)
            .Select(e => new Dictionary<string, string>
            {
                ["diary_date"] = FormatDate(e.Date),
                ["diary_work"] = e.WorkDescription,
                ["diary_mark"] = e.SupervisorMark ?? "выполнено"
            })
            .ToList();
    }

    public static List<Dictionary<string, string>> BuildTaskRows(List<Domain.Entities.Task> tasks)
    {
        return tasks
            .OrderBy(t => t.Deadline)
            .Select(t => new Dictionary<string, string>
            {
                ["task_title"] = t.Title,
                ["task_description"] = t.Description ?? "",
                ["task_deadline"] = FormatDate(t.Deadline),
                ["task_status"] = StatusLabels.Task(t.Status)
            })
            .ToList();
    }

    private static string? ShortNameFromFull(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return fullName;
        }
        if (parts.Length == 1)
        {
            return parts[0];
        }
        var last = parts[0];
        var initials = string.Join(" ", parts.Skip(1).Select(p => p[0] + "."));
        return $"{last} {initials}";
    }
}
