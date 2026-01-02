// Path: Configuration/IAppSettingsReader.cs
namespace VecTool.Core.Models
{
    /// <summary>
    /// Abstraction to read application settings for testability.
    /// </summary>
    public interface IAppSettingsReader
    {
        string? Get(string key);
    }
}
