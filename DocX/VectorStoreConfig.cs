namespace DocXHandler;

public class VectorStoreConfig
{
    public List<string> FolderPaths { get; set; } = new List<string>();
    public List<string> ExcludedFiles { get; set; } = new List<string>();
    public List<string> ExcludedFolders { get; set; } = new List<string>();
}