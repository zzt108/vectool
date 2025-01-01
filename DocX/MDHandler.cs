﻿using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;
using System.Collections.Generic;

namespace DocXHandler
{
    public class MDHandler : FileHandlerBase
    {
        public void ExportSelectedFolders(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
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

        private void ProcessFile(string file, StreamWriter writer, List<string> excludedFiles, List<string> excludedFolders)
        {
            string fileName = Path.GetFileName(file);
            if (IsFileExcluded(fileName, excludedFiles) || !IsFileValid(file, null))
            {
                log.Debug($"Skipping excluded file: {file}");
                return;
            }

            string content = GetFileContent(file);
            DateTime lastModified = File.GetLastWriteTime(file);
            writer.WriteLine($"## File: {fileName} Time:{lastModified}");
            writer.WriteLine(content);
        }

        private void WriteFolderName(StreamWriter writer, string folderName)
        {
            writer.WriteLine($"# Folder: {folderName}");
        }
    }
}