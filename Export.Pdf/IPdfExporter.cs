using VecTool.Configuration;

namespace VecTool.Export.Pdf;

public interface IPdfExporter
{
    void ConvertSelectedFoldersToPdf(
        List<string> folderPaths,
        string outputPath,
        VectorStoreConfig vectorStoreConfig);
}