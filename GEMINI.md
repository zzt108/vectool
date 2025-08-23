# Project: C# Test Automation Framework

## General Instructions:
- Always use **NUnit** as the test framework
- Use **Shouldly** for all assertions
- Follow SOLID principles in all code examples
- Write tests that are readable, maintainable, and fast

## Communication Style

- **Tone:** Casual, friendly, and humorous, sarcastic. In the chat window you talk to me in Hungarian.
- **Approach:** Explain concepts like you're teaching a senior developer
- **Certainty Rating:** Always rate your confidence (1-10) and be transparent about uncertainties
- **Language:** All generated code, comments, and UI elements must be in **English**
  
## Coding Style for C# Tests:
- Use 4 spaces for indentation
- Test method names should be descriptive: `Should_ReturnExpectedResult_When_ValidInputProvided`
- Test class names should end with `Tests` (e.g., `UserServiceTests`)
- Always include proper using statements with NUnit and Shouldly
- Use AAA pattern (Arrange, Act, Assert) in all tests

## Specific Instructions:
- When creating new test classes, place them in appropriate folders
- Always mock external dependencies in unit tests
- Use descriptive variable names in tests
- Include setup and teardown methods when needed
- Write both positive and negative test scenarios

## Project Structure:
- DocX/
  - DocXHandler.cs
  - FileHandlerBase.cs
  - FileSizeSummaryHandler.cs
  - GitChangesHandler.cs
  - IDocumentContentValidator.cs
  - MDHandler.cs
  - OpenXmlContentValidator.cs
  - PdfHandler.cs
  - VecToolExtensions.cs
  - VectorStoreConfig.cs
- Docs/
- GitIgnore/
  - Models/
    - GitIgnoreFile.cs
    - GitIgnorePattern.cs
  - Services/
    - GitIgnoreAwareFileProcessor.cs
    - GitIgnoreStatistics.cs
    - HierarchicalGitIgnoreManager.cs
    - IgnoreFileResolver.cs
- Log/
- LogCtx/
  - LogCtxShared/
    - ILogCtxLogger.cs
    - JsonExtensions.cs
    - LogCtx.cs
    - Props.cs
  - NLogShared/
    - CtxLogger.cs
  - Old/
    - LogCtx.cs
    - Nlog/
      - TimeOut.cs
    - SeriLog/
      - zztLog.cs
      - Config/
        - MiscTest.cs
        - SetupFixture.cs
  - SeriLogAdapterTests/
    - SeriLogCtxTests.cs
  - SeriLogShared/
    - CtxLogger.cs
- Media/
- MimeTypes/
  - MimeTypeProvider.cs
- OaiUI/
  - MainForm.cs
  - MainForm.Designer.cs
  - Program.cs
  - WinFormsUserInterface.cs
- oaiVectorStore/
  - FileStoreManager.cs
  - MyOpenAIClient.cs
  - VectorStoreManager.cs
- UnitTests/
  - ConvertSelectedFoldersToDocxTests.cs
  - ConvertSelectedFoldersToMDTests.cs
  - ConvertSelectedFoldersToPdfTests.cs
  - DocTestBase.cs
  - DocXHandlerTests.cs
  - FileSizeSummaryHandlerTests.cs
  - NLogCtxTests.cs
  - SeriLogCtxTests.cs
  - UnitTests.cs
  - GitIgnore/
    - GitIgnorePatternTests.cs
    - GitIgnorePatternTests3.cs
    - HierarchicalIgnoreManagerTests.cs
    - IgnoreFileResolverTests.cs
    - VTIgnoreCaseSensitivityTests.cs

