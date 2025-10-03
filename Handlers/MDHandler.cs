namespace VecTool.Handlers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VecTool.Configuration;
using VecTool.Constants;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using VecTool.Utils;

/// <summary>
/// Handler for exporting folder structures to Markdown format.
/// </summary>
public sealed class MDHandler : FileHandlerBase
{
    private readonly MimeTypeProvider _mimeTypeProvider;

    public MDHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : base(ui, recentFilesManager)
    {
        _mimeTypeProvider = new MimeTypeProvider();
    }

    /// <summary>
    /// Exports selected folders to a single Markdown file.
    /// </summary>
    public void ExportSelectedFolders(
        List<string> folderPaths,
        string outputMarkdownPath,
        VectorStoreConfig vectorStoreConfig)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            throw new ArgumentException("No folders provided", nameof(folderPaths));

        if (string.IsNullOrWhiteSpace(outputMarkdownPath))
            throw new ArgumentException("Output path required", nameof(outputMarkdownPath));

        try
        {
            _ui?.UpdateStatus("Creating Markdown file...");
            _log.Info($"Starting MD export: {folderPaths.Count} folders -> {outputMarkdownPath}");

            var sb = new StringBuilder();

            // Add AI-optimized context
            AddAIOptimizedContext(folderPaths, sb, (builder, content) =>
            {
                builder.AppendLine("```" + Tags.AiContext);
                builder.AppendLine(content);
                builder.AppendLine("```");
                builder.AppendLine();
            });

            // Process each folder
            foreach (var folder in folderPaths)
            {
                ProcessFolder(
                    folder,
                    sb,
                    vectorStoreConfig,
                    ProcessFileToMarkdown,
                    WriteFolderNameToMarkdown,
                    WriteFolderEndToMarkdown);
            }

            File.WriteAllText(outputMarkdownPath, sb.ToString());

            // Register in recent files
            if (_recentFilesManager != null)
            {
                var fi = new FileInfo(outputMarkdownPath);
                _recentFilesManager.RegisterGeneratedFile(
                    outputMarkdownPath,
                    RecentFileType.Md,
                    folderPaths,
                    fi.Exists ? fi.Length : 0);
            }

            _ui?.UpdateStatus($"Markdown created: {outputMarkdownPath}");
            _log.Info($"MD export completed: {outputMarkdownPath}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to export folders to Markdown: {outputMarkdownPath}");
            throw;
        }
    }

    private void ProcessFileToMarkdown(string file, StringBuilder sb, VectorStoreConfig config)
    {
        var fileName = Path.GetFileName(file);
        
        if (IsFileExcluded(fileName, config))
        {
            _log.Trace($"File excluded: {file}");
            return;
        }

        if (!IsFileValid(file, null))
        {
            _log.Trace($"File invalid: {file}");
            return;
        }

        try
        {
            var ext = Path.GetExtension(file);
            var tag = _mimeTypeProvider.GetMdTag(ext);
            
            if (string.IsNullOrWhiteSpace(tag))
                tag = ext.TrimStart('.');

            sb.AppendLine($"## File: {fileName}");
            sb.AppendLine();
            sb.AppendLine($"```" + tag);
            sb.AppendLine(PathHelpers.SafeReadAllText(file));
            sb.AppendLine("```");
            sb.AppendLine();

            _log.Trace($"Processed file: {fileName}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error processing file: {file}");
        }
    }

    private void WriteFolderNameToMarkdown(StringBuilder sb, string folderName)
    {
        sb.AppendLine($"# Folder: {folderName}");
        sb.AppendLine();
    }

    private void WriteFolderEndToMarkdown(StringBuilder sb)
    {
        sb.AppendLine();
    }
}
