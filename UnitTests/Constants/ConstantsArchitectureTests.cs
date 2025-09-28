// File: UnitTests/Constants/ConstantsArchitectureTests.cs 
// Path: UnitTests/Constants/ConstantsArchitectureTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using Constants; // Reference to our new Constants project

namespace UnitTests.Constants
{
    [TestFixture]
    public class ConstantsArchitectureTests
    {
        [Test]
        public void AllTagsShouldHaveConsistentNaming()
        {
            // Arrange & Act
            var tagFields = typeof(Tags).GetFields();

            // Assert
            tagFields.ShouldAllBe(field => !string.IsNullOrWhiteSpace(field.Name));
            tagFields.ShouldAllBe(field => char.IsUpper(field.Name[0]));
        }

        [Test]
        public void ConstantValuesShouldMatchOriginalStrings()
        {
            // Assert - Verify key constants match expected XML patterns
            Tags.FilePath.ShouldBe("file path=\"{0}\"");
            Tags.FileName.ShouldBe("file name=\"{0}\"");
            Tags.FileProperties.ShouldBe("FileProps");
            Tags.AIGuidance.ShouldBe("aiguidance");
        }

        [Test]
        public void NoMagicStringsShouldRemainInConstants()
        {
            // Arrange
            var constantFields = typeof(Tags).GetFields()
                .Concat(typeof(TestStrings).GetFields())
                .Concat(typeof(Attributes).GetFields());

            // Assert
            constantFields.ShouldAllBe(field =>
                field.FieldType == typeof(string) &&
                !string.IsNullOrEmpty((string?)field.GetValue(null)));
        }

        [Test]
        public void TagBuilderShouldEscapeXmlAttributes()
        {
            // Act
            var result = TagBuilder.BuildFilePathTag("C:\\Test<File>&Name.cs");

            // Assert - Fix the chaining issue! 🎯
            result.ShouldContain("&lt;");
            result.ShouldContain("&amp;");
            result.ShouldStartWith("file path=\"");
            result.ShouldEndWith("\"");
        }

        [Test]
        public void TagBuilderShouldThrowOnNullOrEmptyInput()
        {
            // Assert
            Should.Throw<ArgumentException>(() => TagBuilder.BuildFilePathTag(null));
            Should.Throw<ArgumentException>(() => TagBuilder.BuildFilePathTag(""));
            Should.Throw<ArgumentException>(() => TagBuilder.BuildFilePathTag("   "));
        }

        [Test]
        public void AllFileTagsShouldFollowConsistentPattern()
        {
            // Act & Assert
            TagBuilder.BuildFileNameTag("test.cs").ShouldStartWith("file name=\"");
            TagBuilder.BuildFilePathTag("C:\\test").ShouldStartWith("file path=\"");
            TagBuilder.BuildSectionNameTag("TestSection").ShouldStartWith("section name=\"");
        }
    }
}
