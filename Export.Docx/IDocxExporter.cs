using VecTool.Configuration;

namespace VecTool.Export.Docx;

public interface IDocxExporter
{
    void ConvertSelectedFoldersToDocx(
        List<string> folderPaths,
        string outputPath,
        VectorStoreConfig vectorStoreConfig);
}