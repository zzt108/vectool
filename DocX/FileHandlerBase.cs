using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;
using System.Text.RegularExpressions;
using System;

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

        protected bool IsFolderExcluded(string name, VectorStoreConfig vectorStoreConfig)
        {
            return vectorStoreConfig.IsFolderExcluded(name);
        }

        protected bool IsFileExcluded(string fileName, VectorStoreConfig vectorStoreConfig)
        {
            return vectorStoreConfig.IsFileExcluded(fileName);
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
                content = $"```{mdTag}\n{content}\n```";
            }
            return content;
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
            if (IsFolderExcluded(folderName, vectorStoreConfig))
            {
                _log.Trace($"Skipping excluded folder: {folderPath}");
                return;
            }

            // Update the UI status for the current folder
            _ui?.UpdateStatus($"Processing folder: {folderPath}");
            _log.Debug($"Processing folder: {folderPath}");

            writeFolderName(context, folderName);

            string[] files = Directory.GetFiles(folderPath);
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

    }
}