namespace VecTool.VectorStore.OpenAI
{
    /// <summary>
    /// Provider interface for vector store operations.
    /// Enables testability and future provider implementations.
    /// </summary>
    public interface IVectorStoreProvider
    {
        /// <summary>
        /// Creates a new vector store with the specified name.
        /// </summary>
        /// <param name="name">Display name for the vector store.</param>
        /// <returns>The ID of the created vector store.</returns>
        Task<string> CreateVectorStoreAsync(string name);

        /// <summary>
        /// Uploads a file to the specified vector store.
        /// </summary>
        /// <param name="vectorStoreId">Target vector store ID.</param>
        /// <param name="filePath">Local file path to upload.</param>
        /// <returns>True if upload succeeded, false otherwise.</returns>
        Task<bool> UploadFileAsync(string vectorStoreId, string filePath);
    }
}