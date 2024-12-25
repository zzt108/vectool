﻿using oaiVectorStore;
using NLogS = NLogShared;

namespace DocXHandler
{
    public class MDHandler : FileHandlerBase
    {

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
            if (FileHandlerBase.IsFolderExcluded(folderName, excludedFolders))
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
                if (FileHandlerBase.IsFileExcluded(fileName, excludedFiles) || !FileHandlerBase.IsFileValid(file, outputPath))
                {
                    log.Debug($"Skipping excluded file: {file}");
                    continue; // Skip this file
                }
                string content = FileHandlerBase.GetFileContent(file);
                DateTime lastModified = File.GetLastWriteTime(file);

                writer.WriteLine($"## File: {Path.GetFileName(file)} Time:{lastModified}");
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
