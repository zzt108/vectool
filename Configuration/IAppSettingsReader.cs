// Path: Configuration/IAppSettingsReader.cs
namespace VecTool.Configuration
{
    /// <summary>
    /// Abstraction to read application settings for testability.
    /// </summary>
    public interface IAppSettingsReader
    {
        string? Get(string key);
    }
}
