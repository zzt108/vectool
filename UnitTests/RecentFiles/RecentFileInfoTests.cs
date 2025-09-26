// File: UnitTests/RecentFiles/RecentFileInfoTests.cs
// Framework: NUnit + Shouldly (as required)
// Validates: JSON round-trip, missing/corrupt props, file existence.

using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using DocXHandler.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFileInfoTests
    {
        [Test]
        public void RoundTrip_Should_Preserve_Core_Fields()
        {
            // Arrange
            using var tmp = new TempFile(".md", "hello");
            var original = RecentFileInfo.FromPath(
                tmp.Path,
                RecentFileType.Md,
                new[] { Path.GetDirectoryName(tmp.Path) ?? "" },
                new DateTimeOffset(2025, 09, 26, 12, 34, 56, TimeSpan.Zero));

            // Act
            var json = RecentFilesJson.ToJson(new[] { original });
            var back = RecentFilesJson.FromJson(json);

            // Assert
            back.Count.ShouldBe(1);
            var item = back[0];
            item.FilePath.ShouldBe(original.FilePath);
            item.FileType.ShouldBe(RecentFileType.Md);
            item.GeneratedAt.ShouldBe(original.GeneratedAt);
            item.SourceFolders.ShouldContain(Path.GetDirectoryName(tmp.Path));
            item.FileSizeBytes.ShouldBeGreaterThan(0);
        }

        [Test]
        public void Missing_And_Corrupt_Props_Should_Be_Tolerated()
        {
            // Missing sourceFolders and corrupt generatedAt/date => GeneratedAt defaults, sourceFolders empty
            var json = @"[
              {
                ""filePath"": ""C:/nope/doesnotexist.docx"",
                ""fileType"": ""Docx"",
                ""fileSizeBytes"": -123
              }
            ]";

            var items = RecentFilesJson.FromJson(json);
            items.Count.ShouldBe(1);
            var item = items[0];

            item.FilePath.ShouldContain("doesnotexist.docx");
            item.FileType.ShouldBe(RecentFileType.Docx);
            item.SourceFolders.Count.ShouldBe(0);
            item.FileSizeBytes.ShouldBe(0);
            item.GeneratedAt.ShouldNotBe(default(DateTimeOffset));
        }

        [Test]
        public void Exists_Property_Should_Reflect_File_System()
        {
            using var tmp = new TempFile(".pdf", "dummy");
            var info = RecentFileInfo.FromPath(tmp.Path, RecentFileType.Pdf, Array.Empty<string>());

            info.Exists.ShouldBeTrue();

            File.Delete(tmp.Path);
            info.Exists.ShouldBeFalse();
        }

        private sealed class TempFile : IDisposable
        {
            public string Path { get; }

            public TempFile(string extension, string content)
            {
                var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RecentFileInfoTests");
                Directory.CreateDirectory(folder);
                Path = System.IO.Path.Combine(folder, $"{Guid.NewGuid()}{extension}");
                File.WriteAllText(Path, content);
            }

            public void Dispose()
            {
                try { if (File.Exists(Path)) File.Delete(Path); } catch { /* ignore */ }
            }
        }
    }
}
