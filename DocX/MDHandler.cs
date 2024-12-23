﻿using oaiVectorStore;
using NLogS = NLogShared;

namespace DocXHandler
{
    public class MDHandler
    {
        private static NLogS.CtxLogger log = new();

        public static void ExportSelectedFoldersToMarkdown(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
        {
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (string folderPath in folderPaths)
                {
                    ProcessFolderForMarkdown(folderPath, writer, outputPath, excludedFiles, excludedFolders);
                }
            }
        }

        private static void ProcessFolderForMarkdown(string folderPath, StreamWriter writer, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
        {
            string folderName = new DirectoryInfo(folderPath).Name;
            if (excludedFolders.Contains(folderName)) // Accessing the MainForm's excludedFolders
            {
                log.Debug($"Skipping excluded folder: {folderPath}");
                return;
            }

            log.Debug(folderPath);

            // Write folder name
            writer.WriteLine($"# Folder: {folderPath}");

            // Get all text files in the current folder
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                if (excludedFiles.Any(excludedFile => string.Equals(excludedFile, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    log.Debug($"Skipping excluded file: {file}");
                    continue; // Skip this file
                }
                if (file == outputPath)
                {
                    continue;
                }
                // Check MIME type and upload
                string extension = Path.GetExtension(file);
                if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
                {
                    continue;
                }

                if (MimeTypeProvider.IsBinary(extension)) // non text types should be uploaded separately
                {
                    continue;
                }

                // Check if the file content is not empty
                if (new FileInfo(file).Length == 0)
                {
                    continue; // Skip empty files
                }

                string content = File.ReadAllText(file);
                var mdTag = MimeTypeProvider.GetMdTag(extension);
                if (mdTag != null)
                {
                    // Add start and end language tags to the file content
                    content = $"```{mdTag}\n{content}\n```";
                }

                // Write file name and content
                writer.WriteLine($"## File: {Path.GetFileName(file)}");
                writer.WriteLine(content);
            }

            // Recursively process subfolders
            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
            {
                ProcessFolderForMarkdown(subfolder, writer, outputPath, excludedFiles, excludedFolders);
            }
        }
    }
}