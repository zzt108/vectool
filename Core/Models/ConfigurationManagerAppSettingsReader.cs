// Path: Configuration/ConfigurationManagerAppSettingsReader.cs
using System.Configuration;

namespace VecTool.Core.Models
{
    /// <summary>
    /// Default appSettings reader backed by System.Configuration.
    /// </summary>
    public sealed class ConfigurationManagerAppSettingsReader : IAppSettingsReader
    {
        public string? Get(string key) => ConfigurationManager.AppSettings[key];
    }
}
