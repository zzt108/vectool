﻿using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;
using System.Collections.Generic;

namespace DocXHandler
{
    public class MDHandler(IUserInterface? ui) : FileHandlerBase(ui)
    {

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