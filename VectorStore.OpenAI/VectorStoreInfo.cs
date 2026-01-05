namespace VecTool.VectorStore.OpenAI
{
    /// <summary>
    /// Represents metadata about a vector store.
    /// Used for listing and display purposes.
    /// </summary>
    public sealed class VectorStoreInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int FileCount { get; set; }
    }
}