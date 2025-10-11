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
        private readonly IVersionProvider _v;
        public AboutVersionAdapter(IVersionProvider v) => _v = v;

        public string ApplicationName => _v.ApplicationName;
        public string AssemblyVersion => _v.AssemblyVersion;
        public string FileVersion => _v.FileVersion;
        public string InformationalVersion => _v.InformationalVersion;
        public string CommitShort => _v.CommitShort;
        public DateTime BuildTimestampUtc => _v.BuildTimestampUtc;
    }
}
