// Path: UnitTests/Constants/ConstantsArchitectureTests.cs
using NUnit.Framework;
using Shouldly;
using VecTool.Constants;

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
            Tags.FilePath.ShouldBe("path=\"{0}\"");
            Tags.FileName.ShouldBe("file name=\"{0}\"");
            Tags.FileProperties.ShouldBe("fileproperties");
            Tags.AIGuidance.ShouldBe("aiguidance");
        }

        [Test]
        public void TagBuilderShouldEscapeXmlAttributes()
        {
            // Act - Use TestStrings.DangerousValue which contains XML-dangerous chars!
            var result = TagBuilder.BuildFilePathTag(TestStrings.DangerousValue);

            // Assert - Now we should see the escaped versions
            result.ShouldContain("&lt;");    // from '<' in dangerous value
            result.ShouldContain("&amp;");   // from '&' in dangerous value
            result.ShouldContain("&quot;");  // from '"' in dangerous value
            result.ShouldStartWith("path=\"");
            result.ShouldEndWith("\"");
        }

        [Test]
        public void TagBuilderShouldThrowOnNullOrEmptyInput()
        {
            // Assert
            Should.Throw<ArgumentException>(() => TagBuilder.BuildFilePathTag(null!));
            Should.Throw<ArgumentException>(() => TagBuilder.BuildFilePathTag(string.Empty));
            Should.Throw<ArgumentException>(() => TagBuilder.BuildFilePathTag("   "));
        }

        [Test]
        public void AllFileTagsShouldFollowConsistentPattern()
        {
            // Act & Assert
            TagBuilder.BuildFileNameTag("test.cs").ShouldStartWith("file name=\"");
            TagBuilder.BuildFilePathTag("C:\\safe\\path").ShouldStartWith("path=\"");
            TagBuilder.BuildSectionNameTag("TestSection").ShouldStartWith("section name=\"");
        }

        [Test]
        public void EscapingShouldWorkCorrectlyWithTestData()
        {
            // Act - Test the escaping directly
            var escaped = TagBuilder.EscapeXmlAttribute(TestStrings.DangerousValue);

            // Assert - Should match the expected escaped form
            escaped.ShouldBe(TestStrings.EscapedDangerousValue);
        }

        [Test]
        public void NoMagicStringsShouldRemainInConstants()
        {
            // Arrange
            var constantFields = typeof(Tags).GetFields()
                .Concat(typeof(TestStrings).GetFields())
                .Concat(typeof(Attributes).GetFields());

            // Assert
            constantFields.ShouldAllBe(field => field.FieldType == typeof(string));
            constantFields.ShouldAllBe(field => !string.IsNullOrEmpty((string?)field.GetValue(null)));
        }
    }
}
