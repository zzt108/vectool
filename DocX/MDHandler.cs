﻿using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;
using System.Collections.Generic;

namespace DocXHandler
{
    public class MDHandler(IUserInterface? ui) : FileHandlerBase(ui)
    {

        public void ExportSelectedFolders(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            try
            {
                _ui?.WorkStart("Exporting to MD", folderPaths);
                var work = 0;
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    foreach (string folderPath in folderPaths)
                    {
                        _ui?.UpdateProgress(work++);
                        ProcessFolder(
                            folderPath,
                            writer,
                            vectorStoreConfig,
                            ProcessFile,
                            WriteFolderName);
                    }
                }
            }
            finally
            {
                _ui?.WorkFinish();
            }
        }

        protected override void ProcessFile(string file, StreamWriter writer, VectorStoreConfig vectorStoreConfig)
        {

            if (IsFileExcluded(file, vectorStoreConfig) || !IsFileValid(file, null))
            {
                _log.Trace($"Skipping excluded file: {file}");
                return;
            }

            var relativePath = Path.GetRelativePath(vectorStoreConfig.CommonRootPath, file).Replace('\\', '/');

            string content = GetFileContent(file);
            DateTime lastModified = File.GetLastWriteTime(file);
            writer.WriteLine($"## File: {relativePath} Time:{lastModified}");
            writer.WriteLine(content);
        }

        protected override void WriteFolderName(StreamWriter writer, string folderName)
        {
            writer.WriteLine($"# Folder: {folderName}");
        }
    }
}