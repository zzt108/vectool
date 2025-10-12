// Path: UI/VecTool.UI.WinUI/About/AboutVersionAdapter.cs
// Phase 3.1: Data binding adapter for IVersionProvider → XAML

using System;
using VecTool.Core.Versioning;

namespace VecTool.UI.WinUI.About
{
    /// <summary>
    /// Adapter to expose IVersionProvider properties for XAML data binding.
    /// WinUI 3 requires public properties for {Binding} syntax; this wraps the interface.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// var provider = new AssemblyVersionProvider();
    /// var adapter = new AboutVersionAdapter(provider);
    /// aboutPage.DataContext = adapter;
    /// </code>
    /// </remarks>
    public sealed class AboutVersionAdapter
    {
        private readonly IVersionProvider versionProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutVersionAdapter"/> class.
        /// </summary>
        /// <param name="provider">The version provider to wrap. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is null.</exception>
        public AboutVersionAdapter(IVersionProvider provider)
        {
            versionProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Gets the application name (e.g., "VecTool").
        /// </summary>
        public string ApplicationName => versionProvider.ApplicationName;

        /// <summary>
        /// Gets the assembly version (e.g., "4.0.0.0").
        /// </summary>
        public string AssemblyVersion => versionProvider.AssemblyVersion;

        /// <summary>
        /// Gets the file version (e.g., "4.0.1.2345").
        /// </summary>
        public string FileVersion => versionProvider.FileVersion;

        /// <summary>
        /// Gets the informational version (e.g., "4.0.1-beta+abc1234").
        /// </summary>
        public string InformationalVersion => versionProvider.InformationalVersion;

        /// <summary>
        /// Gets the short Git commit hash (first 7 characters, e.g., "abc1234").
        /// </summary>
        public string CommitShort => versionProvider.CommitShort;

        /// <summary>
        /// Gets the build timestamp in UTC, formatted as "Build yyyy-MM-dd HH:mm UTC".
        /// </summary>
        /// <remarks>
        /// Example output: "Build 2025-10-12 06:15 UTC"
        /// </remarks>
        public string BuildTimestampUtc
        {
            get
            {
                var timestamp = versionProvider.BuildTimestampUtc;
                return $"Build {timestamp:yyyy-MM-dd HH:mm} UTC";
            }
        }
    }
}
