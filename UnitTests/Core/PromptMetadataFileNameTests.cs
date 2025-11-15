using NUnit.Framework;
using Shouldly;
using VecTool.Core.Models;

namespace VecTool.Core.Tests
{
    [TestFixture]
    public sealed class PromptMetadataFileNameTests
    {
        [Test]
        public void BuildFileName_WithVersion_UsesFullPattern()
        {
            var fileName = PromptMetadata.BuildFileName("PROMPT", "1.0", "analyzer", ".md");

            fileName.ShouldBe("PROMPT-1.0-analyzer.md");
        }

        [Test]
        public void BuildFileName_WithoutVersion_OmitsVersionSegment()
        {
            var fileName = PromptMetadata.BuildFileName("GUIDE", "", "usage", "md");

            fileName.ShouldBe("GUIDE-usage.md");
        }

        [Test]
        public void BuildFileName_TrimsInputs_AndNormalizesExtension()
        {
            var fileName = PromptMetadata.BuildFileName("  SPACE ", " 2.1 ", " new-feature ", " yaml ");

            fileName.ShouldBe("SPACE-2.1-new-feature.yaml");
        }

        [Test]
        public void BuildFileName_RequiresTypeAndName()
        {
            Should.Throw<ArgumentException>(() =>
                PromptMetadata.BuildFileName("", "1.0", "name", ".md"));

            Should.Throw<ArgumentException>(() =>
                PromptMetadata.BuildFileName("PROMPT", "1.0", "", ".md"));
        }
    }
}
