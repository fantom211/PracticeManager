namespace PracticeManager.Api.Services;

/// <summary>
/// Historical notification text is preserved, but a link to a soft-deleted document falls back to
/// the document list instead of leading to a dead detail page.
/// </summary>
public static class NotificationLinkResolver
{
    public const string FallbackLink = "/documents";

    public static HashSet<Guid> ParseDocumentIds(IEnumerable<string?> links)
    {
        var ids = new HashSet<Guid>();
        foreach (var link in links)
        {
            if (TryGetDocumentId(link, out var id))
            {
                ids.Add(id);
            }
        }
        return ids;
    }

    public static bool TryGetDocumentId(string? link, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(link))
        {
            return false;
        }

        const string prefix = "/documents/";
        if (!link.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = link[prefix.Length..];
        var end = rest.IndexOfAny(['?', '#', '/']);
        var candidate = end >= 0 ? rest[..end] : rest;
        return Guid.TryParse(candidate, out id);
    }

    public static string? Resolve(string? link, IReadOnlySet<Guid> visibleDocumentIds)
    {
        return TryGetDocumentId(link, out var id) && !visibleDocumentIds.Contains(id)
            ? FallbackLink
            : link;
    }
}
