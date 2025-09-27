// File: Tests/RecentFiles/FileRecentFilesStoreTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using DocXHandler.RecentFiles;

namespace Tests.RecentFiles
{
    [TestFixture]
    public class FileRecentFilesStoreTests
    {
        private string _root = default!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "VecTool_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                try { Directory.Delete(_root, true); } catch { /* ignore */ }
            }
        }

        [Test]
        public void Read_When_Not_Exists_Should_Return_Null()
        {
            var cfg = new RecentFilesConfig(10, 15, _root);
            var store = new FileRecentFilesStore(cfg);
            store.Read().ShouldBeNull();
        }

        [Test]
        public void Write_Then_Read_Should_RoundTrip()
        {
            var cfg = new RecentFilesConfig(10, 15, _root);
            var store = new FileRecentFilesStore(cfg);
            var json = "[{\"filePath\":\"c:/x.txt\",\"generatedAt\":\"2025-09-27T00:00:00Z\",\"fileType\":0,\"sourceFolders\":[],\"fileSizeBytes\":1}]";

            store.Write(json);
            var read = store.Read();

            read.ShouldNotBeNull();
            read.ShouldBe(json);
        }
    }
}
