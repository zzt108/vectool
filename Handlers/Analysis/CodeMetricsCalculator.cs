namespace VecTool.Handlers.Analysis;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VecTool.Handlers.Traversal;

/// <summary>
/// Calculates code metrics (LOC, complexity, patterns) for files.
/// </summary>
public sealed class CodeMetricsCalculator
{
    public FileMetrics Calculate(string filePath, List<string> folderPaths)
    {
        var text = PathHelpers.SafeReadAllText(filePath);
        var ext = Path.GetExtension(filePath);
        var name = Path.GetFileName(filePath);
        var root = PathHelpers.FindOwningRoot(folderPaths, filePath);
        var rel = PathHelpers.MakeRelativeSafe(root, filePath);

        var fi = new FileInfo(filePath);
        var size = fi.Exists ? fi.Length : 0L;
        var lastModified = fi.Exists ? fi.LastWriteTime : DateTime.MinValue;

        var loc = CountLines(text);
        var codeLines = CountCodeLines(text, ext);
        var classes = CountClasses(text, ext);
        var methods = CountMethods(text, ext);
        var complexity = EstimateComplexity(codeLines, methods, text);
        var patterns = DetectPatterns(text, ext).ToList();
        var longMethods = CountLongMethods(text, ext, 40);
        var todos = CountTodos(text);
        var catches = CountCatches(text);

        return new FileMetrics
        {
            Name = name,
            Path = rel,
            Ext = ext,
            Lang = ext.TrimStart('.'),
            SizeBytes = size,
            Loc = loc,
            CodeLines = codeLines,
            Classes = classes,
            Methods = methods,
            LastModified = lastModified,
            Complexity = complexity,
            Patterns = patterns,
            HasTests = false, // TODO: Implement test detection
            Tests = new List<string>(),
            LongMethods = longMethods,
            Todos = todos,
            Catches = catches
        };
    }

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var lines = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') lines++;
        }
        return lines;
    }

    private static int CountCodeLines(string text, string ext)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var lines = text.Split('\n');
        return lines.Count(l =>
        {
            var s = l.Trim();
            if (s.Length == 0) return false;
            if (s.StartsWith("//")) return false;
            if (s.StartsWith("/*") || s.StartsWith("*") || s.StartsWith("*/")) return false;
            return true;
        });
    }

    private static int CountClasses(string text, string ext)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return Regex.Matches(text, @"\bclass\s+").Count;
    }

    private static int CountMethods(string text, string ext)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return Regex.Matches(text, @"\b[A-Za-z][A-Za-z0-9]*\s*\(").Count;
    }

    private static string EstimateComplexity(int codeLines, int methods, string text)
    {
        var score = (codeLines / 200.0) + (methods / 20.0);
        if (score < 1.0) return "Low";
        if (score < 2.0) return "Medium";
        return "High";
    }

    private static IEnumerable<string> DetectPatterns(string text, string ext)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var hits = new List<string>();
        if (text.Contains("IDisposable", StringComparison.Ordinal))
            hits.Add("DisposePattern");
        if (text.Contains("IOptions", StringComparison.Ordinal))
            hits.Add("Options");
        if (text.Contains("ILogger", StringComparison.Ordinal) || 
            text.Contains("NLog", StringComparison.Ordinal))
            hits.Add("Logging");
        if (text.Contains("async ", StringComparison.Ordinal))
            hits.Add("Async");
        if (text.Contains("IServiceCollection", StringComparison.Ordinal))
            hits.Add("DependencyInjection");
        if (text.Contains("SOLID", StringComparison.Ordinal))
            hits.Add("SOLID");

        return hits;
    }

    private static int CountLongMethods(string text, string ext, int thresholdLines)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var methodMatches = Regex.Matches(text, @"\b[A-Za-z][A-Za-z0-9]*\s*\((?<body>[^}]*})");
        var count = 0;
        foreach (Match m in methodMatches)
        {
            var body = m.Groups["body"].Value;
            var lines = CountLines(body);
            if (lines > thresholdLines) count++;
        }
        return count;
    }

    private static int CountTodos(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return Regex.Matches(text, @"\bTODO\b", RegexOptions.IgnoreCase).Count;
    }

    private static int CountCatches(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return Regex.Matches(text, @"\bcatch\b").Count;
    }
}

/// <summary>
/// Data model for file metrics.
/// </summary>
public sealed class FileMetrics
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Ext { get; init; } = string.Empty;
    public string Lang { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public int Loc { get; init; }
    public int CodeLines { get; init; }
    public int Classes { get; init; }
    public int Methods { get; init; }
    public DateTime LastModified { get; init; }
    public string Complexity { get; init; } = "Low";
    public List<string> Patterns { get; init; } = new();
    public bool HasTests { get; init; }
    public List<string> Tests { get; init; } = new();
    public int LongMethods { get; init; }
    public int Todos { get; init; }
    public int Catches { get; init; }

    public string ToXml()
    {
        // XML generation implementation
        return $"<file name=\"{Name}\" path=\"{Path}\" />";
    }
}
