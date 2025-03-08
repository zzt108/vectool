using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;
using System.Text.RegularExpressions;
using System;

namespace DocXHandler
{
    public abstract class FileHandlerBase
    {
        protected static NLogS.CtxLogger log = new();

        protected bool IsFolderExcluded(string name, List<string> excludedList)
        {
            return excludedList.Contains(name);
        }

        public virtual void ExportSelectedFolders(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
        {
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (string folderPath in folderPaths)
                {
                    ProcessFolder(
                        folderPath,
                        writer,
                        excludedFiles,
                        excludedFolders,
                        ProcessFile,
                        WriteFolderName);
                }
            }
        }

        protected bool IsFileExcluded(string fileName, List<string> excludedFiles)
        {
            foreach (var pattern in excludedFiles)
            {
                string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        protected bool IsFileValid(string file, string outputPath)
        {
            if (file == outputPath)
            {
                return false;
            }

            string extension = Path.GetExtension(file);
            if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream" || MimeTypeProvider.IsBinary(extension))
            {
                return false;
            }

            return new FileInfo(file).Length > 0;
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

        protected void ProcessFolder<T>(
            string folderPath,
            T context,
            List<string> excludedFiles,
            List<string> excludedFolders,
            Action<string, T, List<string>, List<string>> processFile,
            Action<T, string> writeFolderName,
            Action<T> writeFolderEnd = null)
        {
            string folderName = new DirectoryInfo(folderPath).Name;
            if (IsFolderExcluded(folderName, excludedFolders))
            {
                log.Trace($"Skipping excluded folder: {folderPath}");
                return;
            }

            log.Debug($"Processing folder: {folderPath}");

            writeFolderName(context, folderName);

            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                processFile(file, context, excludedFiles, excludedFolders);
            }

            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
            {
                ProcessFolder(subfolder, context, excludedFiles, excludedFolders, processFile, writeFolderName, writeFolderEnd);
            }

            writeFolderEnd?.Invoke(context);
        }

        protected virtual void ProcessFile(string file, StreamWriter writer, List<string> excludedFiles, List<string> excludedFolders)
        {
        }

        protected virtual void WriteFolderName(StreamWriter writer, string folderName)
        {
        }

    }
}