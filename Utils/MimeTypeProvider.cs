namespace VecTool.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Provides MIME type detection for files based on extension.
/// </summary>
public class MimeTypeProvider
{
    private readonly Dictionary<string, string> _mimeTypes;
    private readonly Dictionary<string, string> _mdTags;
    private readonly HashSet<string> _newExtensions;

    private const string DEFAULT_MIME_TYPE = "application/octet-stream";
    private const string MIME_TYPES_JSON = "mimeTypes.json";
    private const string MD_TAGS_JSON = "mdTags.json";
    private const string NEW_EXTENSIONS_JSON = "newExtensions.json";

    public MimeTypeProvider()
    {
        _mimeTypes = LoadJsonToDictionary(MIME_TYPES_JSON);
        _mdTags = LoadJsonToDictionary(MD_TAGS_JSON);
        _newExtensions = LoadJsonToHashSet(NEW_EXTENSIONS_JSON);
    }

    /// <summary>
    /// Gets the MIME type for a given file extension.
    /// </summary>
    /// <param name="extension">File extension (with or without leading dot)</param>
    /// <returns>MIME type string</returns>
    public string GetMimeType(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return DEFAULT_MIME_TYPE;

        var ext = extension.StartsWith(".") ? extension : $".{extension}";
        return _mimeTypes.TryGetValue(ext.ToLowerInvariant(), out var mimeType) 
            ? mimeType 
            : DEFAULT_MIME_TYPE;
    }

    /// <summary>
    /// Gets the Markdown tag for a given file extension.
    /// </summary>
    /// <param name="extension">File extension (with or without leading dot)</param>
    /// <returns>Markdown tag or empty string if not found</returns>
    public string GetMdTag(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var ext = extension.StartsWith(".") ? extension : $".{extension}";
        return _mdTags.TryGetValue(ext.ToLowerInvariant(), out var tag) 
            ? tag 
            : string.Empty;
    }

    /// <summary>
    /// Determines if the extension is a new/recent addition.
    /// </summary>
    /// <param name="extension">File extension (with or without leading dot)</param>
    /// <returns>True if extension is new, false otherwise</returns>
    public bool IsNewExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var ext = extension.StartsWith(".") ? extension : $".{extension}";
        return _newExtensions.Contains(ext.ToLowerInvariant());
    }

    private static Dictionary<string, string> LoadJsonToDictionary(string jsonFileName)
    {
        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jsonFileName);
            if (!File.Exists(jsonPath))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(jsonPath);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) 
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static HashSet<string> LoadJsonToHashSet(string jsonFileName)
    {
        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jsonFileName);
            if (!File.Exists(jsonPath))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(jsonPath);
            var list = JsonConvert.DeserializeObject<List<string>>(json);
            return list != null 
                ? new HashSet<string>(list, StringComparer.OrdinalIgnoreCase) 
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
