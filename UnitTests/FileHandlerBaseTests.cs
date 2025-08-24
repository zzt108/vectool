using DocXHandler;
using Shouldly;

[TestFixture]
public class FileHandlerBaseTests
{
    private TestableFileHandler _handler;
    private VectorStoreConfig _config;
    private string _testDirectory;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileHandlerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _handler = new TestableFileHandler(null);
        _config = new VectorStoreConfig(new List<string> { _testDirectory });
    }

    [Test]
    public void IsFileValid_Should_Return_False_For_Empty_Files()
    {
        // Arrange
        var emptyFile = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(emptyFile, "");

        // Act
        var result = _handler.TestIsFileValid(emptyFile);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void IsFileValid_Should_Return_False_For_Binary_Files()
    {
        // Arrange
        var binaryFile = Path.Combine(_testDirectory, "test.png");
        File.WriteAllBytes(binaryFile, new byte[] { 0xFF, 0xD8, 0xFF }); // PNG header

        // Act
        var result = _handler.TestIsFileValid(binaryFile);

        // Assert
        result.ShouldBeFalse();
    }

    // Concrete implementation for testing abstract class
    private class TestableFileHandler : FileHandlerBase
    {
        public TestableFileHandler(IUserInterface? ui) : base(ui) { }

        public bool TestIsFileValid(string filePath) => IsFileValid(filePath);
        public string TestGetFileContent(string file) => GetFileContent(file);
    }
}
