namespace VecTool.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// Provides MIME type detection for files based on extension.
/// </summary>
public class MimeTypeProvider
{
    private static readonly Dictionary<string, string> _mimeTypes;
    private static readonly Dictionary<string, string> _newExtensions;
    private static readonly Dictionary<string, string> _mdTags;

    static MimeTypeProvider()
    {
        _mimeTypes = LoadDictionaryFromFile("Config\\mimeTypes.json");
        _newExtensions = LoadDictionaryFromFile("Config\\newExtensions.json");
        _mdTags = LoadDictionaryFromFile("Config\\mdTags.json");
    }

    private static Dictionary<string, string> LoadDictionaryFromFile(string fileName)
    {
        var jsonContent = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent)
               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static string? GetMimeType(string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
        {
            return null;
        }

        if (!fileExtension.StartsWith("."))
        {
            fileExtension = $".{fileExtension}";
        }

        return _mimeTypes.TryGetValue(fileExtension, out var mimeType) ? mimeType : null;
    }

    public static string? GetNewExtension(string fileExtension)
    {
        _newExtensions.TryGetValue(fileExtension, out string? newExtension);
        return newExtension;
    }

    public static string? GetMdTag(string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return null;

        _mdTags.TryGetValue(fileExtension, out string? mdTag);

        if (mdTag == null)
        {
            mdTag = fileExtension.TrimStart('.'); // Use the extension itself as the tag if not found in mdTags.json
        }

        return mdTag;
    }

    private static string? GetExtension(string fileExtension)
    {
        _mdTags.TryGetValue(fileExtension, out string? mdTag);
        return mdTag;
    }

}


