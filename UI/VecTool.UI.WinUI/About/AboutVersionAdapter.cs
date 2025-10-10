// Path: src/UI/VecTool.UI.WinUI/About/AboutVersionAdapter.cs

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

namespace VecTool.UI.WinUI.About
{
    public interface IVersionProvider
    {
        string ApplicationName { get; }
        string AssemblyVersion { get; }
        string FileVersion { get; }
        string InformationalVersion { get; }
        string CommitShort { get; }
        DateTime BuildTimestampUtc { get; }
    }

    internal sealed class AboutVersionAdapter
    {
        private readonly IVersionProvider _versions;

        public AboutVersionAdapter(IVersionProvider versions) => _versions = versions;

        public string ApplicationName => _versions.ApplicationName;
        public string AssemblyVersion => _versions.AssemblyVersion;
        public string FileVersion => _versions.FileVersion;
        public string InformationalVersion => _versions.InformationalVersion;
        public string CommitShort => _versions.CommitShort;
        public DateTime BuildTimestampUtc => _versions.BuildTimestampUtc;
    }
}
