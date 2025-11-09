using NUnit.Framework;

public abstract class FileSystemTestBase
{
    protected string TestRoot { get; private set; } = null!;

    [SetUp]
    public void BaseSetUp()
    {
        TestRoot = Path.Combine(Path.GetTempPath(), $"VecToolTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(TestRoot);
    }

    [TearDown]
    public void BaseTearDown()
    {
        if (Directory.Exists(TestRoot))
            Try.DeleteDirectory(TestRoot);
    }

    protected string CreateTestFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(TestRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }
}
