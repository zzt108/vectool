using DocumentFormat.OpenXml.Packaging;
using NLog;
using NLog.Config;
using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.Export.Docx;

namespace VecTool.Export.Docx.Tests;

[TestFixture]
public class DocxExportHandlerTests
{
    private string _testDir = default!;

    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // Kényszerítsd a Seq target regisztrációját kódból
            //LogManager.Setup().SetupExtensions(ext =>
            //{
            //    ext.RegisterTarget<NLog.Targets.Seq.SeqTarget>("Seq");
            //});
        }
    }

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DocxTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Test]
    public void ConvertSelectedFoldersToDocx_ShouldCreateValidDocx()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test.cs");
        File.WriteAllText(testFile, "public class Test { }");

        var outputPath = Path.Combine(_testDir, "output.docx");
        var config = new VectorStoreConfig(_testDir);
        var handler = new DocxExportHandler(null, null);

        // Act
        handler.ConvertSelectedFoldersToDocx(new List<string> { _testDir }, outputPath, config);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();

        // Verify DOCX structure
        using var doc = WordprocessingDocument.Open(outputPath, false);
        doc.MainDocumentPart.ShouldNotBeNull();
        doc.MainDocumentPart!.Document.ShouldNotBeNull();
    }

    [Test]
    public void ConvertSelectedFoldersToDocx_ShouldRespectExclusions()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, ".gitignore"), "*.log");
        File.WriteAllText(Path.Combine(_testDir, "code.cs"), "code");
        File.WriteAllText(Path.Combine(_testDir, "debug.log"), "log content");

        var outputPath = Path.Combine(_testDir, "output.docx");
        var config = new VectorStoreConfig(_testDir);
        var handler = new DocxExportHandler(null, null);

        // Act
        handler.ConvertSelectedFoldersToDocx(new List<string> { _testDir }, outputPath, config);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();

        // Open and check content doesn't contain "debug.log"
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;
        bodyText.ShouldNotContain("debug.log");
        bodyText.ShouldContain("code.cs");
    }

    [Test]
    public void ConvertSelectedFoldersToDocx_ShouldHandleEmptyFolder()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var outputPath = Path.Combine(_testDir, "output.docx");
        var config = new VectorStoreConfig(_testDir);
        var handler = new DocxExportHandler(null, null);

        // Act & Assert
        Should.NotThrow(() =>
            handler.ConvertSelectedFoldersToDocx(new List<string> { emptyDir }, outputPath, config));

        File.Exists(outputPath).ShouldBeTrue();
    }
}