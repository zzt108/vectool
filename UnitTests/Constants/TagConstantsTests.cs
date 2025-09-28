// Path: UnitTests/Constants/TagConstantsTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using Constants;

namespace UnitTests.Constants
{
    [TestFixture]
    public class TagConstantsTests
    {
        [Test]
        public void AllXmlTagsShouldBePresent()
        {
            Tags.TableOfContents.ShouldNotBeNullOrWhiteSpace();
            Tags.Section.ShouldNotBeNullOrWhiteSpace();
            Tags.File.ShouldNotBeNullOrWhiteSpace();
            Tags.CrossReferences.ShouldNotBeNullOrWhiteSpace();
            Tags.CodeMetaInfo.ShouldNotBeNullOrWhiteSpace();

            Tags.SectionName.ShouldContain("{0}");
            Tags.FileName.ShouldContain("{0}");
            Tags.FilePath.ShouldContain("{0}");
            Tags.FileExtension.ShouldContain("{0}");
        }

        [Test]
        public void TagValuesShouldMatchExpectedFormat()
        {
            var name = TagBuilder.BuildFileNameTag(TestStrings.SampleFileName);
            name.ShouldBe($"file name=\"{TestStrings.SampleFileName}\"");

            var path = TagBuilder.BuildFilePathTag(TestStrings.SampleRelativePath);
            path.ShouldBe($"path=\"{TestStrings.SampleRelativePath}\"");

            var section = TagBuilder.BuildSectionNameTag(TestStrings.SampleSection);
            section.ShouldBe($"section name=\"{TestStrings.SampleSection}\"");
        }

        [Test]
        public void EscapeShouldHandleDangerousValues()
        {
            var escaped = TagBuilder.EscapeXmlAttribute(TestStrings.DangerousValue);
            escaped.ShouldBe(TestStrings.EscapedDangerousValue);

            var ext = TagBuilder.BuildExtensionTag(TestStrings.SampleExtension);
            ext.ShouldBe($"ext=\"{TestStrings.SampleExtension}\"");
        }

        [Test]
        public void NoConstantShouldBeEmpty()
        {
            foreach (var f in typeof(Tags).GetFields())
            {
                var val = f.GetValue(null) as string;
                (val ?? string.Empty).ShouldNotBeNullOrWhiteSpace();
            }
        }
    }
}
