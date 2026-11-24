using Microsoft.Net.Http.Headers;

namespace PracticeManager.Api.Services;

/// <summary>
/// Builds RFC 5987 Content-Disposition headers so Cyrillic template/document names survive downloads.
/// </summary>
public static class DownloadResponseBuilder
{
    public static string ContentDisposition(string fileName, bool inline)
    {
        var header = new ContentDispositionHeaderValue(inline ? "inline" : "attachment")
        {
            FileNameStar = fileName
        };
        return header.ToString();
    }
}
