# Project Structure

This document outlines the folder and file organization of the **VecToolDev** solution.

```
VecToolDev/
├── DocX/
│   ├── DocXHandler.cs
│   ├── FileHandlerBase.cs
│   ├── FileSizeSummaryHandler.cs
│   ├── GitChangesHandler.cs
│   ├── IDocumentContentValidator.cs
│   ├── MDHandler.cs
│   ├── OpenXmlContentValidator.cs
│   ├── PdfHandler.cs
│   ├── VecToolExtensions.cs
│   └── VectorStoreConfig.cs
├── GitIgnore/
│   ├── GitIgnore.csproj
│   ├── Models/
│   │   ├── GitIgnoreFile.cs
│   │   └── GitIgnorePattern.cs
│   └── Services/
│       ├── FileProcessorOptions
│       ├── GitIgnoreAwareFileProcessor.cs
│       ├── GitIgnoreStatistics.cs
│       └── HierarchicalGitIgnoreManager.cs
├── Log/
│   └── Log.csproj
├── LogCtx/
│   ├── LogCtxShared/
│   │   ├── ILogCtxLogger.cs
│   │   ├── JsonExtensions.cs
│   │   ├── LogCtx.cs
│   │   └── Props.cs
│   ├── NLogShared/
│   │   └── CtxLogger.cs
│   ├── SeriLogShared/
│   │   └── CtxLogger.cs
│   └── Old/ (deprecated implementations)
├── MimeTypes/
│   ├── MimeTypeProvider.cs
│   ├── Config/
│   │   ├── mdTags.json
│   │   ├── mimeTypes.json
│   │   └── newExtensions.json
│   └── MimeTypes.csproj
├── OaiUI/
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   ├── Program.cs
│   ├── WinFormsUserInterface.cs
│   └── oaiUI.csproj
├── oaiVectorStore/
│   ├── FileStoreManager.cs
│   ├── MyOpenAIClient.cs
│   ├── VectorStoreManager.cs
│   └── oaiVectorStore.csproj
├── UnitTests/
│   ├── ConvertSelectedFoldersToDocxTests.cs
│   ├── ConvertSelectedFoldersToMDTests.cs
│   ├── ConvertSelectedFoldersToPdfTests.cs
│   ├── DocTestBase.cs
│   ├── DocXHandlerTests.cs
│   ├── FileSizeSummaryHandlerTests.cs
│   ├── NLogCtxTests.cs
│   ├── SeriLogCtxTests.cs
│   ├── UnitTests.cs
│   └── UnitTests.csproj
└── README.md
```
