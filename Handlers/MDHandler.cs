using VecTool.Configuration;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    public class MDHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager) : FileHandlerBase(ui, recentFilesManager)
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
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
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
                }
                if (_recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    _recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.AllSourceMd,
                        folderPaths,
                        fileInfo.Length
                    );
                }

            }
            finally
            {
                ui?.WorkFinish();
            }
        }

        protected override void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {
            string fileName = Path.GetFileName(file);
            if (IsFileExcluded(fileName, vectorStoreConfig) || !IsFileValid(file, null))
            {
                log.Trace($"Skipping excluded file: {file}");
                return;
            }

            string content = GetFileContent(file);
            DateTime lastModified = File.GetLastWriteTime(file);
            writer.WriteLine($"## File: {fileName} Time:{lastModified}");
            writer.WriteLine(content);
        }

        protected override void WriteFolderName(StreamWriter writer, string folderName)
        {
            writer.WriteLine($"# Folder: {folderName}");
        }
    }
}