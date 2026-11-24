using DocumentFormat.OpenXml.Wordprocessing;

namespace PracticeManager.Api.Services.DocumentGeneration;

/// <summary>
/// Expands {{#block}} ... {{/block}} table row templates. A row containing the start marker and a
/// row containing the end marker bracket one or more template rows. The template rows are cloned
/// once per item, tokens are substituted, and the markers plus the original template are removed.
/// If the item list is empty, the whole block (markers and template rows) is removed.
/// </summary>
public static class RepeatingBlockRenderer
{
    public static void ExpandBlock(
        Table table,
        string blockName,
        IReadOnlyList<IReadOnlyDictionary<string, string>> items)
    {
        var rows = table.Elements<TableRow>().ToList();
        var startMarker = "{{#" + blockName + "}}";
        var endMarker = "{{/" + blockName + "}}";

        var startIndex = rows.FindIndex(r => RowContainsText(r, startMarker));
        var endIndex = rows.FindIndex(r => RowContainsText(r, endMarker));
        if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
        {
            return;
        }

        var startRow = rows[startIndex];
        var endRow = rows[endIndex];
        var templateRows = rows.Skip(startIndex + 1).Take(endIndex - startIndex - 1).ToList();

        if (templateRows.Count == 0)
        {
            startRow.Remove();
            endRow.Remove();
            return;
        }

        var anchor = endRow;
        foreach (var item in items)
        {
            foreach (var templateRow in templateRows)
            {
                var clone = (TableRow)templateRow.CloneNode(true);
                foreach (var paragraph in clone.Descendants<Paragraph>().ToList())
                {
                    PlaceholderReplacer.ReplaceInParagraph(paragraph, item);
                    PlaceholderReplacer.ExpandLineBreaks(paragraph);
                }
                anchor.InsertBeforeSelf(clone);
            }
        }

        foreach (var templateRow in templateRows)
        {
            templateRow.Remove();
        }
        startRow.Remove();
        endRow.Remove();
    }

    private static bool RowContainsText(TableRow row, string marker)
    {
        return row.Descendants<Text>().Any(t => t.Text.Contains(marker));
    }
}
