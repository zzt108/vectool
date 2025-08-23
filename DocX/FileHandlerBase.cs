using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Text;
using GitIgnore.Services;

namespace DocXHandler
{
    public abstract class FileHandlerBase
    {
        protected static NLogS.CtxLogger _log = new();
        protected readonly IUserInterface? _ui;

        protected FileHandlerBase(IUserInterface? ui)
        {
            _ui = ui;
        }

        public static string RelativePath(string commonRootPath, string file)
        {
            if (string.IsNullOrWhiteSpace(commonRootPath) || string.IsNullOrWhiteSpace(file))
            {
                return file;                 
            }

            return Path.GetRelativePath(commonRootPath, file)
                                   .Replace('\\', '/');
        }

        protected bool IsFileValid(string filePath, string? outputPath)
        {
            // Skip output file itself
            if (outputPath != null && filePath == outputPath)
            {
                return false;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);

                // Skip empty files
                if (fileInfo.Length == 0)
                {
                    return false;
                }

                // Skip binary files or check MIME type
                string extension = Path.GetExtension(filePath);
                if (MimeTypeProvider.IsBinary(extension))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected string GetFileContent(string file)
        {
            string content = File.ReadAllText(file);
            var mdTag = MimeTypeProvider.GetMdTag(Path.GetExtension(file));
            if (mdTag != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"```{mdTag}");
                sb.AppendLine(content);
                sb.AppendLine("```");
                return sb.ToString();
            }
            return content;
        }

        public virtual IEnumerable<string> GetProcessableFiles(string directory, VectorStoreConfig _vectorStoreConfig)
        {
            // OLD: return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

            return directory.EnumerateFilesRespectingGitIgnore("*.*", _vectorStoreConfig);
        }

        /// <summary>
        /// Recursively processes a folder and its subfolders while updating UI progress and status.
        /// </summary>
        /// <typeparam name="T">The type of context used for writing output (for example, a StreamWriter or a QuestPDF ColumnDescriptor).</typeparam>
        /// <param name="folderPath">The folder to process.</param>
        /// <param name="context">A context for the output.</param>
        /// <param name="vectorStoreConfig">The configuration containing exclusion logic.</param>
        /// <param name="processFile">A delegate to process individual files.</param>
        /// <param name="writeFolderName">A delegate to write the folder name to the output.</param>
        /// <param name="writeFolderEnd">An optional delegate to mark the end of folder processing.</param>
        protected void ProcessFolder<T>(
            string folderPath,
            T context,
            VectorStoreConfig vectorStoreConfig,
            Action<string, T, VectorStoreConfig> processFile,
            Action<T, string> writeFolderName,
            Action<T> writeFolderEnd = null)
        {
            string folderName = new DirectoryInfo(folderPath).Name;

            // Update the UI status for the current folder
            _ui?.UpdateStatus($"Processing folder: {folderPath}");
            _log.Debug($"Processing folder: {folderPath}");

            writeFolderName(context, folderName);

            // string[] files = Directory.GetFiles(folderPath);
            string[] files = GetProcessableFiles(folderPath, vectorStoreConfig).ToArray();
            foreach (string file in files)
            {
                try
                {
                processFile(file, context, vectorStoreConfig);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error processing file: {file}");
                    throw;
                }
            }

            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
            {
                ProcessFolder(subfolder, context, vectorStoreConfig, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        protected virtual void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
        }

        protected virtual void WriteFolderName(StreamWriter writer, string folderName)
        {
        }

        #region AI-Optimized Context and Metadata

        /// <summary>
        /// Adds project summary and AI guidance to the beginning of processed output
        /// </summary>
        protected void AddAIOptimizedContext<T>(List<string> folderPaths, T context, Action<T, string> writeContent)
        {
            // Generate AI guidance
            string aiGuidance = GenerateAIGuidance();
            writeContent(context, aiGuidance);

            // Generate project context
            string projectContext = GenerateProjectContext(folderPaths);
            writeContent(context, projectContext);
        }

        // DocX/FileHandlerBase.cs
        /// <summary>
        /// Generates project context information for Al to better understand the code
        /// </summary>
        protected string GenerateProjectContext(List<string> folderPaths)
        {
            var projectInfo = new StringBuilder();
            // Basic project information
            projectInfo.AppendLine("<project_summary>");
            projectInfo.AppendLine($" <timestamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</timestamp>");
            projectInfo.AppendLine($" <folder_count>{folderPaths.Count}</folder_count>");

            // Try to detect project type and language
            var projectLanguage = DetectProjectLanguage(folderPaths);
            if (!string.IsNullOrEmpty(projectLanguage))
            {
                projectInfo.AppendLine($" <primary_language>{projectLanguage}</primary_language>");
            }

            // Include project structure overview
            projectInfo.AppendLine(" <directory_structure>");

            // Create a properly configured VectorStoreConfig with excluded folders from app.config
            var vectorStoreConfig = VectorStoreConfig.FromAppConfig();

            foreach (var folderPath in folderPaths)
            {
                GenerateDirectoryStructure(folderPath, projectInfo, " ", vectorStoreConfig);
            }

            projectInfo.AppendLine(" </directory_structure>");
            projectInfo.AppendLine("</project_summary>");
            return projectInfo.ToString();
        }

        /// <summary>
        /// Attempts to detect the primary programming language of the project
        /// </summary>
        protected string DetectProjectLanguage(List<string> folderPaths)
        {
            var extensionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths)
            {
                // var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                var files = GetProcessableFiles(folderPath, VectorStoreConfig.FromAppConfig());
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(extension))
                    {
                        if (!extensionCounts.ContainsKey(extension))
                        {
                            extensionCounts[extension] = 0;
                        }
                        extensionCounts[extension]++;
                    }
                }
            }

            // Map common extensions to languages
            var languageMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".cs", "C#" },
                { ".java", "Java" },
                { ".kt", "Kotlin" },
                { ".js", "JavaScript" },
                { ".ts", "TypeScript" },
                { ".py", "Python" },
                { ".rb", "Ruby" },
                { ".php", "PHP" },
                { ".go", "Go" },
                { ".cpp", "C++" },
                { ".c", "C" },
                { ".swift", "Swift" },
                { ".html", "HTML" },
                { ".css", "CSS" },
                { ".sql", "SQL" },
                { ".md", "Markdown" }
            };

            // Find the most common extension that maps to a known language
            var mostCommonExtension = extensionCounts
                .Where(x => languageMapping.ContainsKey(x.Key))
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            if (mostCommonExtension.Key != null && languageMapping.ContainsKey(mostCommonExtension.Key))
            {
                return languageMapping[mostCommonExtension.Key];
            }

            return string.Empty;
        }

        /// <summary>
        /// Recursively generates a directory structure overview
        /// </summary>
        // DocX/FileHandlerBase.cs
        private void GenerateDirectoryStructure(string path, StringBuilder output, string indent, VectorStoreConfig vectorStoreConfig)
        {
            var dirInfo = new DirectoryInfo(path);
            output.AppendLine($"{indent}<directory name=\"{dirInfo.Name}\">");
            try
            {
                // Add subdirectories
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    GenerateDirectoryStructure(subDir, output, indent + " ", vectorStoreConfig);
                }
                // Add file count information
                // var files = Directory.GetFiles(path);
                var files = GetProcessableFiles(path, vectorStoreConfig);
                if (files.Any())
                {
                    output.AppendLine($"{indent} <file_count>{files.Count()}</file_count>");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error processing directory structure for {path}");
            }
            output.AppendLine($"{indent}</directory>");
        }

        /// <summary>
        /// Generates AI-specific guidance for better comprehension of the code
        /// </summary>
        protected string GenerateAIGuidance()
        {
            var guidance = new StringBuilder();
            guidance.AppendLine("<ai_guidance>");
            guidance.AppendLine("  <purpose>This document contains source code and project structure from a development project for AI analysis.</purpose>");
            guidance.AppendLine("  <instructions>");
            guidance.AppendLine("    <instruction>The project structure is preserved in a hierarchical format with XML-like tags.</instruction>");
            guidance.AppendLine("    <instruction>Each file is wrapped in XML-like tags with path and timestamp metadata.</instruction>");
            guidance.AppendLine("    <instruction>Code blocks are formatted with language tags for proper syntax highlighting.</instruction>");
            guidance.AppendLine("    <instruction>Directory structure is provided at the beginning to aid understanding relationships.</instruction>");
            guidance.AppendLine("    <instruction>Binary files and other unsupported content types are excluded.</instruction>");
            guidance.AppendLine("  </instructions>");
            guidance.AppendLine("  <suggested_analysis>");
            guidance.AppendLine("    <task>Analyze the code architecture and design patterns</task>");
            guidance.AppendLine("    <task>Identify potential improvements, technical debt, or bugs</task>");
            guidance.AppendLine("    <task>Explain relationships between different components</task>");
            guidance.AppendLine("    <task>Provide a summary of the application's functionality</task>");
            guidance.AppendLine("  </suggested_analysis>");
            guidance.AppendLine("</ai_guidance>");

            return guidance.ToString();
        }

        /// <summary>
        /// Generates file metadata in a consistent format across all handlers
        /// </summary>
        protected string GenerateFileMetadata(string filePath, VectorStoreConfig vectorStoreConfig)
        {
            string relativePath = RelativePath(vectorStoreConfig.CommonRootPath, filePath);
            DateTime lastModified = File.GetLastWriteTime(filePath);
            string extension = Path.GetExtension(filePath);
            long fileSize = new FileInfo(filePath).Length;

            var metadata = new StringBuilder();
            metadata.AppendLine("<file_metadata>");
            metadata.AppendLine($"  <path>{relativePath}</path>");
            metadata.AppendLine($"  <last_modified>{lastModified:yyyy-MM-dd HH:mm:ss}</last_modified>");
            metadata.AppendLine($"  <language>{MimeTypeProvider.GetMdTag(extension) ?? extension.TrimStart('.')}</language>");
            metadata.AppendLine($"  <size_bytes>{fileSize}</size_bytes>");
            metadata.AppendLine("</file_metadata>");

            return metadata.ToString();
        }

        /// <summary>
        /// Enhanced version of GetFileContent that includes XML-style tags and metadata
        /// </summary>
        protected string GetEnhancedFileContent(string file, VectorStoreConfig vectorStoreConfig)
        {
            try
            {
                string content = File.ReadAllText(file);
                string extension = Path.GetExtension(file);
                var mdTag = MimeTypeProvider.GetMdTag(extension);

                // Generate enhanced content with XML-style tags
                string relativePath = RelativePath(vectorStoreConfig.CommonRootPath, file);
                DateTime lastModified = File.GetLastWriteTime(file);

                var sb = new StringBuilder();
                // sb.AppendLine($"<file path=\"{relativePath}\" last_modified=\"{lastModified:yyyy-MM-dd HH:mm:ss}\">");
                sb.AppendLine(GenerateFileMetadata(file, vectorStoreConfig));

                // Add content with appropriate syntax highlighting
                if (mdTag != null)
                {
                    sb.AppendLine($"```{mdTag}");
                    sb.AppendLine(content);
                    sb.AppendLine("```");
                }
                else
                {
                    sb.AppendLine(content);
                }

                // sb.AppendLine("</file>");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error in GetEnhancedFileContent for file {file}");
                return $"<error>Failed to process file: {Path.GetFileName(file)} {ex.Message}</error>";
            }
        }

        #endregion
    }
}