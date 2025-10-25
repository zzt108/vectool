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

    public static string GetMimeType(string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
        {
            return "application/octet-stream";
        }

        if (!fileExtension.StartsWith("."))
        {
            fileExtension = $".{fileExtension}";
        }

        return _mimeTypes.TryGetValue(fileExtension, out var mimeType) ? mimeType : "application/octet-stream";
    }

    public static string? GetNewExtension(string fileExtension)
    {
        _newExtensions.TryGetValue(fileExtension, out string? newExtension);
        return newExtension;
    }

    public static string? GetMdTag(string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return string.Empty;

        _mdTags.TryGetValue(fileExtension, out string? mdTag);

        if (mdTag == null)
        {
            mdTag = fileExtension.TrimStart('.'); // Use the extension itself as the tag if not found in mdTags.json
        }

        return mdTag;
    }

    public static bool IsBinary(string fileExtension)
    {
        _mdTags.TryGetValue(fileExtension, out string? mdTag);
        // all known binary files are in mdTags, so if it is not in mdTags, it is text
        return mdTag is null ? false : mdTag.Equals("application/binary", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a file is binary by checking mdTags.json first, 
    /// then falling back to heuristic detection for unknown extensions.
    /// </summary>
    /// <param name="fileExtension">The file extension (e.g., ".ttf", ".bin")</param>
    /// <param name="filePath">The full path to the file for content inspection if needed</param>
    /// <returns>True if the file is binary, false if it's text</returns>
    public static bool IsBinary(string fileExtension, string? filePath)
    {
        // First check mdTags.json (authoritative)
        _mdTags.TryGetValue(fileExtension, out string? mdTag);

        if (mdTag != null)
        {
            // Known extension - use mdTags.json value
            return mdTag.Equals("application/binary", StringComparison.OrdinalIgnoreCase);
        }

        // Unknown extension - use heuristic detection
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            // Cannot probe file - assume text (safer for unknown extensions)
            return false;
        }

        return DetectBinaryByContent(filePath);
    }

    /// <summary>
    /// Heuristic binary detection: reads first 8KB and checks for null bytes.
    /// </summary>
    private static bool DetectBinaryByContent(string filePath)
    {
        try
        {
            const int bufferSize = 8192; // 8KB sample
            var buffer = new byte[bufferSize];

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int bytesRead = fs.Read(buffer, 0, bufferSize);

            // Check for null bytes (0x00) - strong indicator of binary content
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0x00)
                {
                    return true; // Found null byte = binary
                }
            }

            // No null bytes found = likely text
            return false;
        }
        catch
        {
            // Cannot read file - assume text (safer fallback)
            return false;
        }
    }
}


