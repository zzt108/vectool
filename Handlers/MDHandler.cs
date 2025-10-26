using VecTool.Configuration;
using VecTool.RecentFiles;
using VecTool.Utils;

namespace VecTool.Handlers
{
    public class MDHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : FileHandlerBase(ui, recentFilesManager)
    {
        /// <summary>
        /// Async wrapper for ExportSelectedFolders to enable better parallelism.
        /// </summary>
        public Task ExportSelectedFoldersAsync(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            return Task.Run(() => ExportSelectedFolders(folderPaths, outputPath, vectorStoreConfig));
        }

        public void ExportSelectedFolders(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            try
            {
                ui?.WorkStart("Exporting to MD", folderPaths);
                var work = 0;

                using StreamWriter writer = new StreamWriter(outputPath);

                foreach (string folderPath in folderPaths)
                {
                    ui?.UpdateProgress(work++);
                    ProcessFolder(
                        folderPath,
                        writer,
                        vectorStoreConfig,
                        ProcessFile,
                        WriteFolderName);
                }

                if (recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.Codebase_Md,
                        folderPaths,
                        fileInfo.Length);
                }
            }
            finally
            {
                ui?.WorkFinish();
            }
        }

        protected override void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
            if (!Traversal.FileValidator.ShouldIncludeInExport(file, vectorStoreConfig))
            {
                log.Trace($"Skipping file (excluded by centralized filter): {file}");
                return;
            }

            string content = GetFileContent(file);
            DateTime lastModified = File.GetLastWriteTime(file);

            writer.WriteLine($"## File: {Path.GetFileName(file)} (Time:{lastModified})");
            writer.WriteLine($"``` {MimeTypeProvider.GetMdTag(Path.GetExtension(file))}");
            writer.WriteLine(content);
            writer.WriteLine("```");
        
            writer.WriteLine(); 
        }

        protected override void WriteFolderName(StreamWriter writer, string folderName)
        {
            writer.WriteLine($"# Folder: {folderName}");
        }
    }
}
