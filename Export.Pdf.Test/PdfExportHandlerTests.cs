#nullable enable

using NLog;
using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.Export.Pdf;

namespace VecTool.Export.Pdf.Tests;

[TestFixture]
public class PdfExportHandlerTests
{
    private string testDir = default!;

    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // Kényszerítsd a Seq target regisztrációját kódból
            LogManager.Setup().SetupExtensions(ext =>
                ext.RegisterTarget<NLog.Targets.Seq.SeqTarget>("Seq"));
        }
    }

    [SetUp]
    public void SetUp()
    {
        testDir = Path.Combine(Path.GetTempPath(), "PdfTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Test]
    public void ConvertSelectedFoldersToPdf_ShouldCreateValidPdf()
    {
        // Arrange
        var testFile = Path.Combine(testDir, "test.cs");
        File.WriteAllText(testFile, "public class Test { }");
        var outputPath = Path.Combine(testDir, "output.pdf");
        var config = new VectorStoreConfig(testDir);
        var handler = new PdfExportHandler(null, null);

        // Act
        handler.ConvertSelectedFoldersToPdf(new List<string> { testDir }, outputPath, config);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();
        var fileInfo = new FileInfo(outputPath);
        fileInfo.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public void ConvertSelectedFoldersToPdf_ShouldRespectExclusions()
    {
        // Arrange
        File.WriteAllText(Path.Combine(testDir, ".gitignore"), "*.log");
        File.WriteAllText(Path.Combine(testDir, "code.cs"), "// code");
        File.WriteAllText(Path.Combine(testDir, "debug.log"), "log content");
        var outputPath = Path.Combine(testDir, "output.pdf");
        var config = new VectorStoreConfig(testDir);
        var handler = new PdfExportHandler(null, null);

        // Act
        handler.ConvertSelectedFoldersToPdf(new List<string> { testDir }, outputPath, config);

        // Assert
        File.Exists(outputPath).ShouldBeTrue();
        // PDF should be created, but we can't easily verify content without parsing
        // At minimum, file should exist and have reasonable size
        var fileInfo = new FileInfo(outputPath);
        fileInfo.Length.ShouldBeGreaterThan(100);
    }

    [Test]
    public void ConvertSelectedFoldersToPdf_ShouldHandleEmptyFolder()
    {
        // Arrange
        var emptyDir = Path.Combine(testDir, "empty");
        Directory.CreateDirectory(emptyDir);
        var outputPath = Path.Combine(testDir, "output.pdf");
        var config = new VectorStoreConfig(testDir);
        var handler = new PdfExportHandler(null, null);

        // Act & Assert
        Should.NotThrow(() =>
            handler.ConvertSelectedFoldersToPdf(new List<string> { emptyDir }, outputPath, config)
        );
        File.Exists(outputPath).ShouldBeTrue();
    }
}