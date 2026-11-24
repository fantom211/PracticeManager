using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PracticeManager.Tests;

/// <summary>
/// Builds small in-memory DOCX documents for renderer tests, so tests do not depend on the
/// real templates in templates/.
/// </summary>
internal static class DocxTestHelpers
{
    public static byte[] BuildDocument(Action<Body> configureBody, bool withFooterPageField = true)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();
            configureBody(body);

            var sectPr = new SectionProperties(
                new PageSize { Width = 11910, Height = 16850 },
                new PageMargin { Top = 1120, Right = 708, Bottom = 280, Left = 1275, Header = 720, Footer = 720, Gutter = 0 });

            if (withFooterPageField)
            {
                var footerPart = mainPart.AddNewPart<FooterPart>();
                var footerId = mainPart.GetIdOfPart(footerPart);
                footerPart.Footer = BuildFooterWithPageField();
                footerPart.Footer.Save();

                sectPr.Append(new FooterReference { Type = HeaderFooterValues.Default, Id = footerId });
            }

            body.Append(sectPr);
            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static Footer BuildFooterWithPageField()
    {
        var paragraph = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
            new Run(new Text("1")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
        return new Footer(paragraph);
    }

    public static Paragraph SimpleParagraph(string text)
    {
        return new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    /// <summary>Builds a paragraph whose text is split across several runs, e.g. "{{" / "token" / "}}".</summary>
    public static Paragraph SplitRunParagraph(params string[] parts)
    {
        var paragraph = new Paragraph();
        foreach (var part in parts)
        {
            paragraph.Append(new Run(new Text(part) { Space = SpaceProcessingModeValues.Preserve }));
        }
        return paragraph;
    }

    public static Table SimpleTable(params string[][] rows)
    {
        var table = new Table();
        foreach (var row in rows)
        {
            var tr = new TableRow();
            foreach (var cellText in row)
            {
                tr.Append(new TableCell(SimpleParagraph(cellText)));
            }
            table.Append(tr);
        }
        return table;
    }

    public static string ExtractText(byte[] docxBytes)
    {
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        return string.Concat(body.Descendants<Text>().Select(t => t.Text));
    }

    public static string ExtractFooterText(byte[] docxBytes)
    {
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var footer = doc.MainDocumentPart!.FooterParts.First().Footer;
        return string.Concat(footer.Descendants<Text>().Select(t => t.Text));
    }

    public static bool FooterHasPageField(byte[] docxBytes)
    {
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var footer = doc.MainDocumentPart!.FooterParts.First().Footer;
        return footer.Descendants<FieldCode>().Any(f => f.Text.Contains("PAGE"));
    }

    public static int CountTableRows(byte[] docxBytes)
    {
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        return table.Elements<TableRow>().Count();
    }
}
