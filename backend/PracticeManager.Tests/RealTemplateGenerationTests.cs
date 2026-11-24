using Microsoft.Extensions.Logging.Abstractions;
using PracticeManager.Api.Services.DocumentGeneration;

namespace PracticeManager.Tests;

/// <summary>
/// Exercises the real templates/ files (not synthetic test fixtures) end to end with demo-like
/// values, so a structural regression in the DOCX templates is caught by `dotnet test` without
/// needing the full API or a database.
/// </summary>
public class RealTemplateGenerationTests
{
    private static string TemplatesRoot
    {
        get
        {
            var dir = AppContext.BaseDirectory;
            var current = new DirectoryInfo(dir);
            while (current != null && !Directory.Exists(Path.Combine(current.FullName, "templates")))
            {
                current = current.Parent;
            }
            if (current == null)
            {
                throw new DirectoryNotFoundException("Could not locate the repository's templates/ directory from the test output path.");
            }
            return Path.Combine(current.FullName, "templates");
        }
    }

    private static Dictionary<string, string> DemoValues() => new()
    {
        ["college_name"] = "Московский международный колледж цифровых технологий «Академия ТОП»",
        ["college_address"] = "121170, г. Москва, вн.тер.г. муниципальный округ Дорогомилово, пр-кт Кутузовский, д. 36, стр. 2",
        ["city"] = "Калининград",
        ["year"] = "2027",
        ["student_full_name"] = "Иванов Максим Сергеевич",
        ["student_short_name"] = "Иванов М. С.",
        ["student_dative_full_name"] = "Иванову Максиму Сергеевичу",
        ["student_genitive_full_name"] = "Иванова Максима Сергеевича",
        ["student_group"] = "ИС-21",
        ["student_number"] = "ИС-21-001",
        ["student_course"] = "3",
        ["student_phone"] = "+7 (900) 123-45-67",
        ["student_email"] = "ivanov@example.com",
        ["specialty_code"] = "09.02.07",
        ["specialty_name"] = "Информационные системы и программирование",
        ["practice_type"] = "Производственная",
        ["practice_name"] = "Производственная практика (по профилю специальности)",
        ["professional_module"] = "ПМ.02 Осуществление интеграции программных модулей",
        ["practice_hours"] = "144",
        ["date_start"] = "01.03.2027",
        ["date_end"] = "14.03.2027",
        ["practice_period"] = "01.03.2027 – 14.03.2027",
        ["organization_name"] = "ООО «Альфа»",
        ["organization_address"] = "г. Москва, ул. Примерная, д. 1",
        ["organization_inn"] = "7701234567",
        ["organization_supervisor"] = "Петров С. А.",
        ["organization_supervisor_full_name"] = "Петров Сергей Александрович",
        ["organization_supervisor_position"] = "Руководитель отдела разработки",
        ["college_supervisor"] = "Иванов П. П.",
        ["college_supervisor_full_name"] = "Иванов Петр Петрович",
        ["college_supervisor_position"] = "Преподаватель",
        ["assessment_grade"] = "—",
        ["assessment_result"] = "—",
        ["assessment_date"] = "—",
        ["assessment_comment"] = "",
        ["report_content"] = "Первый абзац демонстрационного отчёта.\nВторой абзац демонстрационного отчёта.",
        ["characteristic_text"] = "Демонстрационный текст характеристики.",
        ["review_text"] = "Демонстрационный текст отзыва руководителя.",
    };

    private static List<RepeatingBlockData> DemoBlocks() => new()
    {
        new("diary_rows", new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["diary_date"] = "03.03.2027", ["diary_work"] = "Изучение документации", ["diary_mark"] = "выполнено" },
            new Dictionary<string, string> { ["diary_date"] = "04.03.2027", ["diary_work"] = "Разработка модуля авторизации", ["diary_mark"] = "выполнено" },
        }),
        new("task_rows", new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["task_title"] = "Изучить документацию", ["task_description"] = "", ["task_deadline"] = "05.03.2027", ["task_status"] = "Принято" },
        })
    };

    [Theory]
    [InlineData("report-pm01.docx")]
    [InlineData("report-pm02.docx")]
    [InlineData("report-pm03.docx")]
    [InlineData("diary.docx")]
    [InlineData("characteristic.docx")]
    [InlineData("individual-plan.docx")]
    public void Render_RealTemplate_ProducesCleanDocument(string templateFileName)
    {
        var path = Path.Combine(TemplatesRoot, templateFileName);
        Assert.True(File.Exists(path), $"Template not found: {path}");

        var templateBytes = File.ReadAllBytes(path);
        var renderer = new DocxTemplateRenderer(NullLogger<DocxTemplateRenderer>.Instance);

        var result = renderer.Render(templateBytes, DemoValues(), DemoBlocks());

        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.DoesNotContain("{{", text);
        Assert.True(DocxTestHelpers.FooterHasPageField(result.Content), $"{templateFileName}: footer PAGE field missing after generation");
    }
}
