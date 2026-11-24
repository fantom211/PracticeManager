using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PracticeManager.Api.Services.DocumentGeneration;

/// <summary>
/// Replaces {{token}} placeholders inside a single paragraph, even when a token is split across
/// multiple w:r runs. The replacement text is written into the first affected run and the other
/// runs that contributed to the token are cleared, preserving the first run's RunProperties.
/// </summary>
public static class PlaceholderReplacer
{
    private static readonly Regex TokenPattern = new("{{([a-zA-Z0-9_#/]+)}}", RegexOptions.Compiled);

    public static List<string> ReplaceInParagraph(Paragraph paragraph, IReadOnlyDictionary<string, string> values)
    {
        var warnings = new List<string>();
        var texts = paragraph.Descendants<Text>().ToList();
        if (texts.Count == 0)
        {
            return warnings;
        }

        var sb = new StringBuilder();
        var spans = new List<(Text Text, int Start, int Length)>();
        foreach (var t in texts)
        {
            var start = sb.Length;
            var value = t.Text;
            sb.Append(value);
            spans.Add((t, start, value.Length));
        }

        var full = sb.ToString();
        var matches = TokenPattern.Matches(full);
        if (matches.Count == 0)
        {
            return warnings;
        }

        // Build the replaced string, keeping track of edits so we can re-map affected runs.
        var result = new StringBuilder();
        var lastIndex = 0;
        var touchedTextSpans = new List<(int Start, int End)>(); // ranges in `full` that are consumed by a token

        foreach (Match m in matches)
        {
            result.Append(full, lastIndex, m.Index - lastIndex);
            var token = m.Groups[1].Value;
            if (values.TryGetValue(token, out var replacement))
            {
                result.Append(replacement);
            }
            else
            {
                warnings.Add($"Unresolved token: {{{{{token}}}}}");
                result.Append(string.Empty);
            }
            touchedTextSpans.Add((m.Index, m.Index + m.Length));
            lastIndex = m.Index + m.Length;
        }
        result.Append(full, lastIndex, full.Length - lastIndex);

        // Simplest correct approach: put the whole rebuilt string into the first Text run that
        // overlaps the first match, clear all other runs that overlap any match, and leave runs
        // untouched by any match as-is. To keep this simple and correct for the common case where
        // the paragraph contains placeholder tokens (and typically little else beyond them), we
        // rebuild per contiguous "untouched vs token" segments and reassign them to the minimal set
        // of existing Text nodes, preserving the first run touched by each token.
        ApplySegments(spans, full, matches, values, warnings);
        return warnings;
    }

    private static void ApplySegments(
        List<(Text Text, int Start, int Length)> spans,
        string full,
        MatchCollection matches,
        IReadOnlyDictionary<string, string> values,
        List<string> warnings)
    {
        // Compute per-character ownership: -1 = literal text, >=0 = index of match replacing it.
        var owner = new int[full.Length];
        Array.Fill(owner, -1);
        for (var mi = 0; mi < matches.Count; mi++)
        {
            var m = matches[mi];
            for (var i = m.Index; i < m.Index + m.Length; i++)
            {
                owner[i] = mi;
            }
        }

        // For each span (original Text run), determine the new text it should hold.
        // A run that fully precedes/follows tokens keeps its literal slice.
        // The first run overlapping a given match gets the replacement text appended once;
        // subsequent runs overlapping the same match are cleared for that portion.
        var matchWritten = new bool[matches.Count];

        foreach (var (text, start, length) in spans)
        {
            if (length == 0)
            {
                continue;
            }

            var newText = new StringBuilder();
            var i = start;
            var end = start + length;
            while (i < end)
            {
                var own = owner[i];
                if (own == -1)
                {
                    // literal run of characters until ownership changes
                    var j = i;
                    while (j < end && owner[j] == -1)
                    {
                        j++;
                    }
                    newText.Append(full, i, j - i);
                    i = j;
                }
                else
                {
                    var m = matches[own];
                    if (!matchWritten[own])
                    {
                        var token = m.Groups[1].Value;
                        newText.Append(values.TryGetValue(token, out var repl) ? repl : string.Empty);
                        matchWritten[own] = true;
                    }
                    // advance past the whole match region within this span
                    var matchEnd = m.Index + m.Length;
                    i = Math.Min(matchEnd, end);
                }
            }

            text.Text = newText.ToString();
            text.Space = SpaceProcessingModeValues.Preserve;
        }
    }

    /// <summary>
    /// Replaces \n with line breaks within the paragraph that contains the given run, inserting
    /// new Break elements after the run holding the multi-line value.
    /// </summary>
    public static void ExpandLineBreaks(Paragraph paragraph)
    {
        foreach (var run in paragraph.Descendants<Run>().ToList())
        {
            var textEl = run.GetFirstChild<Text>();
            if (textEl == null || !textEl.Text.Contains('\n'))
            {
                continue;
            }

            var lines = textEl.Text.Split('\n');
            var props = run.RunProperties?.CloneNode(true) as RunProperties;

            textEl.Text = lines[0];
            textEl.Space = SpaceProcessingModeValues.Preserve;

            OpenXmlElement anchor = run;
            for (var idx = 1; idx < lines.Length; idx++)
            {
                var newRun = new Run();
                if (props != null)
                {
                    newRun.RunProperties = (RunProperties)props.CloneNode(true);
                }
                newRun.AppendChild(new Break());
                newRun.AppendChild(new Text(lines[idx]) { Space = SpaceProcessingModeValues.Preserve });
                anchor.InsertAfterSelf(newRun);
                anchor = newRun;
            }
        }
    }

    public static bool ContainsAnyToken(string text) => TokenPattern.IsMatch(text);
}
