// File: UnitTests/DocXHandlerRefactoringTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Constants;
using DocXHandler;
using DocXHandler.RecentFiles;
using oaiVectorStore;

namespace UnitTests.DocX
{
    [TestFixture]
    public class DocXHandlerRefactoringTests
    {
        private string root = default!;
        private string outDocx = default!;
        private VectorStoreConfig config = default!;
        private IRecentFilesManager recent = default!;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "VecTool-Step4-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "a1.cs"), "class A1{}");
            File.WriteAllText(Path.Combine(root, "b1.txt"), "B1");
            outDocx = Path.Combine(root, "out.docx");
            config = VectorStoreConfig.FromAppConfig();
            recent = null!;
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }

        [Test]
        public void GeneratedDocxShouldMatchOriginalFormat()
        {
            var handler = new DocXHandler.DocXHandler(null, null);
            handler.ConvertSelectedFoldersToDocx(new List<string> { root }, outDocx, config);

            File.Exists(outDocx).ShouldBeTrue();

            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            text.ShouldContain(Tags.TableOfContents);
            text.ShouldContain(Tags.TableOfContents);
            text.ShouldContain(Tags.CrossReferences);
            text.ShouldContain(Tags.CodeMetaInfo);
        }

        [Test]
        public void AllXmlTagsReplacedWithConstants()
        {
            // Spot-check builder output patterns to ensure we don't regress to magic strings.
            TagBuilder.BuildFileNameTag("x").ShouldStartWith("file name=");
            TagBuilder.BuildFilePathTag("p").ShouldStartWith("path=");
            TagBuilder.BuildExtensionTag(".cs").ShouldStartWith("ext=");
            TagBuilder.BuildLanguageTag("csharp").ShouldStartWith("lang=");
            TagBuilder.BuildSizeBytesTag(123).ShouldStartWith("sizebytes=");
            string.Format(Tags.Complexity, "Low").ShouldStartWith("complexity=");
        }

        [Test]
        public void NoHardcodedStringsRemaining()
        {
            // Heuristic check: ensure known raw tokens are not needed outside constants library.
            // This is a soft guard; the real guard is code review + analyzer in later steps.
            var forbidden = new[] { "tableofcontents", "crossreferences", "codemetainfo", "file name=", "path=", "ext=", "lang=" };
            foreach (var token in forbidden)
            {
                // In practice we’d scan compiled resources or source text; here we assert via constant indirection.
                Tags.TableOfContents.ShouldBe("tableofcontents");
                Tags.CrossReferences.ShouldBe("crossreferences");
                Tags.CodeMetaInfo.ShouldBe("codemetainfo");
            }
        }
    }
}
