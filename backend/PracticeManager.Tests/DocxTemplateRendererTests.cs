using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging.Abstractions;
using PracticeManager.Api.Services.DocumentGeneration;

namespace PracticeManager.Tests;

public class DocxTemplateRendererTests
{
    private readonly DocxTemplateRenderer _renderer = new(NullLogger<DocxTemplateRenderer>.Instance);

    [Fact]
    public void Render_ReplacesSimpleTokenInParagraph()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleParagraph("Студент: {{student_full_name}}"));
        });

        var result = _renderer.Render(template, new Dictionary<string, string> { ["student_full_name"] = "Иванов Максим Сергеевич" });

        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.Contains("Студент: Иванов Максим Сергеевич", text);
        Assert.DoesNotContain("{{", text);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Render_ReplacesTokenSplitAcrossRuns()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SplitRunParagraph("Группа: {{stud", "ent_group", "}}"));
        });

        var result = _renderer.Render(template, new Dictionary<string, string> { ["student_group"] = "ИС-21" });

        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.Equal("Группа: ИС-21", text);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Render_ReplacesTokenInsideTableCell()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleTable(
                new[] { "Организация", "{{organization_name}}" }));
        });

        var result = _renderer.Render(template, new Dictionary<string, string> { ["organization_name"] = "ООО «Альфа»" });

        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.Contains("ООО «Альфа»", text);
        Assert.DoesNotContain("{{", text);
    }

    [Fact]
    public void Render_ExpandsRepeatingTableRows()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleTable(
                new[] { "Дата", "Работа", "Отметка" },
                new[] { "{{#diary_rows}}", "", "" },
                new[] { "{{diary_date}}", "{{diary_work}}", "{{diary_mark}}" },
                new[] { "{{/diary_rows}}", "", "" }));
        });

        var items = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["diary_date"] = "03.03.2027", ["diary_work"] = "Изучение документации", ["diary_mark"] = "выполнено" },
            new Dictionary<string, string> { ["diary_date"] = "04.03.2027", ["diary_work"] = "Разработка модуля", ["diary_mark"] = "выполнено" },
            new Dictionary<string, string> { ["diary_date"] = "05.03.2027", ["diary_work"] = "Тестирование", ["diary_mark"] = "выполнено" },
        };

        var result = _renderer.Render(template, new Dictionary<string, string>(), new List<RepeatingBlockData>
        {
            new("diary_rows", items)
        });

        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.Contains("03.03.2027", text);
        Assert.Contains("04.03.2027", text);
        Assert.Contains("05.03.2027", text);
        Assert.DoesNotContain("{{", text);

        // header row + 3 data rows, markers removed
        Assert.Equal(4, DocxTestHelpers.CountTableRows(result.Content));
    }

    [Fact]
    public void Render_RemovesWholeBlock_WhenItemListIsEmpty()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleTable(
                new[] { "№", "Задание" },
                new[] { "{{#task_rows}}", "" },
                new[] { "{{task_title}}", "{{task_deadline}}" },
                new[] { "{{/task_rows}}", "" }));
        });

        var result = _renderer.Render(template, new Dictionary<string, string>(), new List<RepeatingBlockData>
        {
            new("task_rows", new List<IReadOnlyDictionary<string, string>>())
        });

        Assert.Equal(1, DocxTestHelpers.CountTableRows(result.Content)); // header row only
        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.DoesNotContain("{{", text);
    }

    [Fact]
    public void Render_PreservesFooterPageField()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleParagraph("{{college_name}}"));
        }, withFooterPageField: true);

        var result = _renderer.Render(template, new Dictionary<string, string> { ["college_name"] = "Колледж" });

        Assert.True(DocxTestHelpers.FooterHasPageField(result.Content));
    }

    [Fact]
    public void Render_UnresolvedToken_BecomesEmptyAndProducesWarning()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleParagraph("Значение: {{unknown_token}}."));
        });

        var result = _renderer.Render(template, new Dictionary<string, string>());

        var text = DocxTestHelpers.ExtractText(result.Content);
        Assert.Equal("Значение: .", text);
        Assert.DoesNotContain("{{", text);
        Assert.Single(result.Warnings);
        Assert.Contains("unknown_token", result.Warnings[0]);
    }

    [Fact]
    public void Render_SupportsMultiLineValues_AsLineBreaks()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleParagraph("{{report_content}}"));
        });

        var result = _renderer.Render(template, new Dictionary<string, string> { ["report_content"] = "Первый абзац.\nВторой абзац." });

        using var stream = new MemoryStream(result.Content);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Descendants<Paragraph>().First(p => p.InnerText.Contains("Первый"));
        Assert.Contains(paragraph.Descendants<Break>(), b => true);
    }

    [Fact]
    public void Render_NeverMutatesOriginalTemplateBytes()
    {
        var template = DocxTestHelpers.BuildDocument(body =>
        {
            body.Append(DocxTestHelpers.SimpleParagraph("{{student_full_name}}"));
        });
        var originalCopy = template.ToArray();

        _renderer.Render(template, new Dictionary<string, string> { ["student_full_name"] = "Иванов" });

        Assert.Equal(originalCopy, template);
    }
}
