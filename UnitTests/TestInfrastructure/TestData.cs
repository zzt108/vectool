// ✅ NEW - TestInfrastructure/TestData.cs
using VecTool.Configuration;
using VecTool.RecentFiles;

public static class TestData
{
    public static RecentFilesConfig DefaultConfig(string outputPath) =>
        new(200, 30, outputPath);

    public static VectorStoreConfig DefaultVectorStore() =>
        new() { FolderPaths = new List<string> { "." } };

    public static RecentFileInfo SampleFileInfo(string path) =>
        RecentFileInfo.FromPath(
            path,
            RecentFileType.Codebase_Md,
            new[] { Path.GetDirectoryName(path)! },
            DateTimeOffset.UtcNow
        );
}
