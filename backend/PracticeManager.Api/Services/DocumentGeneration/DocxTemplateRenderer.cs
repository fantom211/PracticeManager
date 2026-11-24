using System.Security.Cryptography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PracticeManager.Api.Services.DocumentGeneration;

public record RepeatingBlockData(string Name, IReadOnlyList<IReadOnlyDictionary<string, string>> Items);

public record DocxRenderResult(byte[] Content, string Sha256, IReadOnlyList<string> Warnings);

/// <summary>
/// Renders a DOCX template by replacing {{token}} placeholders and expanding {{#block}} repeating
/// table rows. The source template is never mutated: it is loaded into a MemoryStream copy.
/// </summary>
public class DocxTemplateRenderer(ILogger<DocxTemplateRenderer> logger)
{
    public DocxRenderResult Render(
        byte[] templateBytes,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<RepeatingBlockData>? blocks = null)
    {
        blocks ??= [];
        var warnings = new List<string>();

        using var output = new MemoryStream();
        output.Write(templateBytes, 0, templateBytes.Length);
        output.Position = 0;

        using (var doc = WordprocessingDocument.Open(output, true))
        {
            var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("Шаблон повреждён: отсутствует основная часть документа.");

            // Expand repeating table blocks first, since the block templates may themselves
            // contain simple {{token}} placeholders that get substituted per item.
            foreach (var table in mainPart.Document.Body!.Descendants<Table>().ToList())
            {
                foreach (var block in blocks)
                {
                    RepeatingBlockRenderer.ExpandBlock(table, block.Name, block.Items);
                }
            }

            RenderPart(mainPart.Document.Body!, values, warnings);

            foreach (var headerPart in mainPart.HeaderParts)
            {
                RenderPart(headerPart.Header, values, warnings);
            }

            foreach (var footerPart in mainPart.FooterParts)
            {
                RenderPart(footerPart.Footer, values, warnings);
            }

            mainPart.Document.Save();
        }

        var bytes = output.ToArray();
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        if (warnings.Count > 0)
        {
            logger.LogWarning("DOCX generation produced {Count} warning(s): {Warnings}", warnings.Count, string.Join("; ", warnings));
        }

        return new DocxRenderResult(bytes, sha, warnings);
    }

    private static void RenderPart(DocumentFormat.OpenXml.OpenXmlElement root, IReadOnlyDictionary<string, string> values, List<string> warnings)
    {
        foreach (var paragraph in root.Descendants<Paragraph>().ToList())
        {
            var w = PlaceholderReplacer.ReplaceInParagraph(paragraph, values);
            warnings.AddRange(w);
            PlaceholderReplacer.ExpandLineBreaks(paragraph);
        }
    }
}
