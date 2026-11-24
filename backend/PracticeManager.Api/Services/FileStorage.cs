using System.Security.Cryptography;

namespace PracticeManager.Api.Services;

public class FileValidationException(string message) : Exception(message);

/// <summary>
/// Stores document version files on disk under Storage/documents/{documentId}/v{version}.{ext}.
/// Only relative paths are kept in the database.
/// </summary>
public class FileStorage(IConfiguration configuration, IWebHostEnvironment env, ILogger<FileStorage> logger)
{
    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    public string Root { get; } = Path.GetFullPath(Path.Combine(
        env.ContentRootPath, configuration["Storage:Root"] ?? "Storage/documents"));

    public string TemplatesRoot { get; } = Path.GetFullPath(Path.Combine(
        env.ContentRootPath, configuration["Templates:Root"] ?? "../../templates"));

    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public (string RelativePath, long Size, string Sha256) SaveVersion(Guid documentId, int versionNumber, byte[] content, string extension)
    {
        extension = extension.TrimStart('.').ToLowerInvariant();
        var dir = Path.Combine(Root, documentId.ToString());
        Directory.CreateDirectory(dir);
        var fileName = $"v{versionNumber}.{extension}";
        var fullPath = Path.Combine(dir, fileName);
        File.WriteAllBytes(fullPath, content);

        var relative = Path.Combine(documentId.ToString(), fileName).Replace('\\', '/');
        var sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        logger.LogInformation("Stored document version file {RelativePath} ({Size} bytes)", relative, content.Length);
        return (relative, content.Length, sha);
    }

    public byte[] ReadVersion(string relativePath)
    {
        var fullPath = ResolveSafePath(relativePath);
        return File.ReadAllBytes(fullPath);
    }

    /// <summary>
    /// Resolves a relative path under the storage root and rejects any attempt to escape it.
    /// </summary>
    private string ResolveSafePath(string relativePath)
    {
        if (!IsUnderRoot(Root, relativePath, out var fullPath))
        {
            throw new FileValidationException("Недопустимый путь к файлу.");
        }
        return fullPath;
    }

    /// <summary>
    /// Resolves a template file path under <see cref="TemplatesRoot"/> without allowing traversal
    /// or absolute paths. Returns false for anything outside the templates directory.
    /// </summary>
    public bool TryResolveTemplatePath(string relativePath, out string fullPath)
    {
        return IsUnderRoot(TemplatesRoot, relativePath, out fullPath);
    }

    public string ResolveTemplatePath(string relativePath)
    {
        if (!TryResolveTemplatePath(relativePath, out var fullPath))
        {
            throw new FileValidationException("Недопустимый путь к файлу шаблона.");
        }
        return fullPath;
    }

    private static bool IsUnderRoot(string root, string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(root, normalized));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    public static string SanitizeFileName(string original)
    {
        var name = Path.GetFileName(original);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

    /// <summary>
    /// Validates magic bytes, not just the file extension, and returns the normalized extension and content type.
    /// </summary>
    public static (string Extension, string ContentType) ValidateDocumentBytes(byte[] content, string originalFileName)
    {
        if (content.Length == 0)
        {
            throw new FileValidationException("Файл пустой.");
        }
        if (content.Length > MaxSizeBytes)
        {
            throw new FileValidationException("Размер файла превышает 10 МБ.");
        }

        var ext = Path.GetExtension(originalFileName).TrimStart('.').ToLowerInvariant();

        // DOCX is a ZIP archive that must contain [Content_Types].xml.
        if (ext == "docx")
        {
            if (!IsZip(content) || !ZipContainsContentTypes(content))
            {
                throw new FileValidationException("Файл не является корректным DOCX-документом.");
            }
            return ("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }

        if (ext == "pdf")
        {
            if (content.Length < PdfMagic.Length || !content.AsSpan(0, PdfMagic.Length).SequenceEqual(PdfMagic))
            {
                throw new FileValidationException("Файл не является корректным PDF-документом.");
            }
            return ("pdf", "application/pdf");
        }

        throw new FileValidationException("Поддерживаются только файлы DOCX и PDF.");
    }

    private static bool IsZip(byte[] content)
    {
        return content.Length >= 4 && content[0] == 0x50 && content[1] == 0x4B && (content[2] == 0x03 || content[2] == 0x05 || content[2] == 0x07);
    }

    private static bool ZipContainsContentTypes(byte[] content)
    {
        try
        {
            using var ms = new MemoryStream(content);
            using var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") != null;
        }
        catch
        {
            return false;
        }
    }
}
