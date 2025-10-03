namespace VecTool.Handlers.Analysis;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VecTool.Configuration;
using VecTool.Constants;
using VecTool.Handlers.Traversal;

/// <summary>
/// Generates AI-optimized context blocks: TOC, CrossReferences, CodeMetaInfo.
/// </summary>
public sealed class AiContextGenerator
{
    private readonly CSharpSymbolAnalyzer _symbolAnalyzer;
    private readonly CodeMetricsCalculator _metricsCalculator;

    public AiContextGenerator()
    {
        _symbolAnalyzer = new CSharpSymbolAnalyzer();
        _metricsCalculator = new CodeMetricsCalculator();
    }

    /// <summary>
    /// Generates table of contents XML block.
    /// </summary>
    public string GenerateTableOfContents(List<string> folderPaths)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            return string.Empty;

        var config = new VectorStoreConfig();

        var entries = new List<(string RootName, string FilePath)>();
        var traverser = new FileSystemTraverser(null);

        foreach (var root in folderPaths)
        {
            var rootName = PathHelpers.SafeDirectoryName(root);
            foreach (var file in traverser.EnumerateFilesRespectingExclusions(root, config))
            {
                entries.Add((rootName, file));
            }
        }

        var grouped = entries
            .GroupBy(e => e.RootName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(TagBuilder.Open(Tags.TableOfContents));

        foreach (var group in grouped)
        {
            sb.AppendLine(TagBuilder.OpenWith(
                Tags.Section,
                TagBuilder.BuildSectionNameTag(group.Key)));

            foreach (var filePath in group.Select(e => e.FilePath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(filePath);
                var name = Path.GetFileName(filePath);
                var root = PathHelpers.FindOwningRoot(folderPaths, filePath);
                var rel = PathHelpers.MakeRelativeSafe(root, filePath);

                sb.AppendLine(TagBuilder.SelfClosing(
                    Tags.File,
                    TagBuilder.BuildFileNameTag(name),
                    TagBuilder.BuildFilePathTag(rel),
                    TagBuilder.BuildExtensionTag(ext)));
            }

            sb.AppendLine(TagBuilder.Close(Tags.Section));
        }

        sb.AppendLine(TagBuilder.Close(Tags.TableOfContents));
        return sb.ToString();
    }

    /// <summary>
    /// Generates cross-references XML block.
    /// </summary>
    public string GenerateCrossReferences(List<string> folderPaths)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            return string.Empty;

        var config = new VectorStoreConfig();

        var csFiles = new List<string>();
        var traverser = new FileSystemTraverser(null);

        foreach (var root in folderPaths)
        {
            foreach (var f in traverser.EnumerateFilesRespectingExclusions(root, config))
            {
                if (string.Equals(Path.GetExtension(f), ".cs", StringComparison.OrdinalIgnoreCase))
                    csFiles.Add(f);
            }
        }

        var dependsOn = _symbolAnalyzer.AnalyzeDependencies(csFiles);
        var usedBy = _symbolAnalyzer.InvertDependencyMap(dependsOn);

        var sb = new StringBuilder();
        sb.AppendLine(TagBuilder.Open(Tags.CrossReferences));

        foreach (var f in csFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var root = PathHelpers.FindOwningRoot(folderPaths, f);
            var rel = PathHelpers.MakeRelativeSafe(root, f);
            var name = Path.GetFileName(f);

            dependsOn.TryGetValue(f, out var deps);
            usedBy.TryGetValue(f, out var ub);

            var dependsCsv = deps is { Count: > 0 }
                ? string.Join(",", deps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Select(x => TagBuilder.EscapeXmlAttribute(PathHelpers.MakeRelativeSafe(root, x))))
                : string.Empty;

            var usedByCsv = ub is { Count: > 0 }
                ? string.Join(",", ub.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Select(x => TagBuilder.EscapeXmlAttribute(PathHelpers.MakeRelativeSafe(root, x))))
                : string.Empty;

            var attrs = new List<string> { TagBuilder.BuildFileNameTag(name), TagBuilder.BuildFilePathTag(rel) };

            if (!string.IsNullOrEmpty(dependsCsv))
                attrs.Add(TagBuilder.BuildDependsOnTag(dependsCsv));

            if (!string.IsNullOrEmpty(usedByCsv))
                attrs.Add(TagBuilder.BuildUsedByTag(usedByCsv));

            sb.AppendLine(TagBuilder.SelfClosing(Tags.File, attrs.ToArray()));
        }

        sb.AppendLine(TagBuilder.Close(Tags.CrossReferences));
        return sb.ToString();
    }

    /// <summary>
    /// Generates code meta-info XML block.
    /// </summary>
    public string GenerateCodeMetaInfo(List<string> folderPaths)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            return string.Empty;

        var config = new VectorStoreConfig();

        var files = new List<string>();
        var traverser = new FileSystemTraverser(null);

        foreach (var root in folderPaths)
        {
            foreach (var f in traverser.EnumerateFilesRespectingExclusions(root, config))
                files.Add(f);
        }

        var inner = new StringBuilder();

        foreach (var f in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var metrics = _metricsCalculator.Calculate(f, folderPaths);
            inner.AppendLine(metrics.ToXml());
        }

        var sb = new StringBuilder();
        sb.AppendLine(TagBuilder.Open(Tags.CodeMetaInfo));
        sb.Append(inner.ToString());
        sb.AppendLine(TagBuilder.Close(Tags.CodeMetaInfo));

        // Duplicate for legacy consumers
        sb.AppendLine(TagBuilder.Open(Tags.CodeMetaInfo));
        sb.Append(inner.ToString());
        sb.AppendLine(TagBuilder.Close(Tags.CodeMetaInfo));

        return sb.ToString();
    }
}
