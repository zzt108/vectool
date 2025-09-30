namespace VecTool.Handlers.Analysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VecTool.Handlers.Traversal;

/// <summary>
/// Analyzes C# files for symbol declarations and dependencies.
/// </summary>
public sealed class CSharpSymbolAnalyzer
{
    private static readonly Regex _typeRegex = new(
        @"\b(class|struct|interface|enum)\s+(?<id>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex _methodRegex = new(
        @"\b(public|internal|protected|private)\s+[^=;,]*?\s+(?<id>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Extracts declared symbols (types and methods) from a C# file.
    /// </summary>
    public HashSet<string> ExtractDeclaredSymbols(string filePath)
    {
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var text = PathHelpers.SafeReadAllText(filePath);
        
        if (text.Length == 0)
            return symbols;

        foreach (Match m in _typeRegex.Matches(text))
            symbols.Add(m.Groups["id"].Value);

        foreach (Match m in _methodRegex.Matches(text))
            symbols.Add(m.Groups["id"].Value);

        return symbols;
    }

    /// <summary>
    /// Analyzes dependencies between C# files based on symbol usage.
    /// </summary>
    public Dictionary<string, HashSet<string>> AnalyzeDependencies(List<string> csFiles)
    {
        var declaredByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var f in csFiles)
        {
            var symbols = ExtractDeclaredSymbols(f);
            if (symbols.Count > 0)
                declaredByFile[f] = symbols;
        }

        var filesBySymbol = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in declaredByFile)
        {
            foreach (var sym in kv.Value)
            {
                if (!filesBySymbol.TryGetValue(sym, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    filesBySymbol[sym] = set;
                }
                set.Add(kv.Key);
            }
        }

        var dependsOn = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in csFiles)
        {
            var text = PathHelpers.SafeReadAllText(f);
            if (text.Length == 0) continue;

            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sym in filesBySymbol.Keys)
            {
                if (IsLikelyOwnDeclaration(sym, f, declaredByFile))
                    continue;

                if (WordExists(text, sym))
                {
                    foreach (var target in filesBySymbol[sym])
                    {
                        if (!string.Equals(target, f, StringComparison.OrdinalIgnoreCase))
                            referenced.Add(target);
                    }
                }
            }

            if (referenced.Count > 0)
                dependsOn[f] = referenced;
        }

        return dependsOn;
    }

    /// <summary>
    /// Inverts dependency map to create "used by" relationships.
    /// </summary>
    public Dictionary<string, HashSet<string>> InvertDependencyMap(
        Dictionary<string, HashSet<string>> dependsOn)
    {
        var usedBy = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var src in dependsOn.Keys)
        {
            foreach (var tgt in dependsOn[src])
            {
                if (!usedBy.TryGetValue(tgt, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    usedBy[tgt] = set;
                }
                set.Add(src);
            }
        }

        return usedBy;
    }

    private static bool IsLikelyOwnDeclaration(
        string symbol, 
        string file, 
        Dictionary<string, HashSet<string>> declaredByFile)
    {
        return declaredByFile.TryGetValue(file, out var set) && set.Contains(symbol);
    }

    private static bool WordExists(string text, string symbol)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(symbol))
            return false;

        var pattern = @"\b" + Regex.Escape(symbol) + @"\b";
        return Regex.IsMatch(text, pattern);
    }
}
