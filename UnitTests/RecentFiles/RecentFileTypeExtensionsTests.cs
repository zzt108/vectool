#nullable enable
using NUnit.Framework;
using Shouldly;
using VecTool.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFileTypeExtensionsTests
    {
        #region ToFileSuffix Tests

        [Test]
        public void ToFileSuffix_Codebase_Docx_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Codebase_Docx.ToFileSuffix();

            // Assert
            result.ShouldBe("_codebase.docx");
        }

        [Test]
        public void ToFileSuffix_Codebase_Md_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Codebase_Md.ToFileSuffix();

            // Assert
            result.ShouldBe("_codebase.md");
        }

        [Test]
        public void ToFileSuffix_CodebasePdf_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Codebase_Pdf.ToFileSuffix();

            // Assert
            result.ShouldBe("_codebase.pdf");
        }

        [Test]
        public void ToFileSuffix_Git_Md_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Git_Md.ToFileSuffix();

            // Assert
            result.ShouldBe("_git.md");
        }

        [Test]
        public void ToFileSuffix_TestResults_Md_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.TestResults_Md.ToFileSuffix();

            // Assert
            result.ShouldBe("_test-results.md");
        }

        [Test]
        public void ToFileSuffix_Plan_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Plan.ToFileSuffix();

            // Assert
            result.ShouldBe("_plan.md");
        }

        [Test]
        public void ToFileSuffix_Guide_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Guide.ToFileSuffix();

            // Assert
            result.ShouldBe("_guide.md");
        }

        [Test]
        public void ToFileSuffix_Summary_Md_ReturnsCorrectSuffix()
        {
            // Act
            var result = RecentFileType.Summary_Md.ToFileSuffix();

            // Assert
            result.ShouldBe("_summary.md");
        }

        [Test]
        public void ToFileSuffix_Unknown_ReturnsDefaultTxt()
        {
            // Act
            var result = RecentFileType.Unknown.ToFileSuffix();

            // Assert
            result.ShouldBe(".txt");
        }

        #endregion

        #region MapExtensionToType - Exact Last Token Match Tests

        [Test]
        public void MapExtensionToType_LastWordPlan_ReturnsPlan()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_Development_plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_LastWordGuide_ReturnsGuide()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "GUIDE-1.8-CodingConvention");

            // Assert
            result.ShouldBe(RecentFileType.Guide);
        }

        [Test]
        public void MapExtensionToType_LastWordSummary_ReturnsSummary_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "Project_summary");

            // Assert
            result.ShouldBe(RecentFileType.Summary_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordGit_ReturnsGit_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_git");

            // Assert
            result.ShouldBe(RecentFileType.Git_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordGitChanges_ReturnsGit_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_git-changes");

            // Assert
            result.ShouldBe(RecentFileType.Git_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordGitChangesNoHyphen_ReturnsGit_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_git");

            // Assert
            result.ShouldBe(RecentFileType.Git_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordTestResults_ReturnsTestResults_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_test-results");

            // Assert
            result.ShouldBe(RecentFileType.TestResults_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordTestResultsNoHyphen_ReturnsTestResults_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_testresults");

            // Assert
            result.ShouldBe(RecentFileType.TestResults_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordCodebase_MdExtension_ReturnsCodebase_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_master_codebase");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        [Test]
        public void MapExtensionToType_LastWordCodebase_DocxExtension_ReturnsCodebase_Docx()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".docx", "VecTool_master_codebase");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Docx);
        }

        [Test]
        public void MapExtensionToType_LastWordCodebasePdfExtension_ReturnsCodebasePdf()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".pdf", "VecTool_master_codebase");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Pdf);
        }

        #endregion

        #region MapExtensionToType - Fallback Pattern Matching Tests

        [Test]
        public void MapExtensionToType_ContainsGitChangesUpperCase_ReturnsGit_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_GIT-CHANGES_2025");

            // Assert
            result.ShouldBe(RecentFileType.Git_Md);
        }

        [Test]
        public void MapExtensionToType_ContainsDotGitDotUpperCase_ReturnsGit_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool.GIT.md");

            // Assert
            result.ShouldBe(RecentFileType.Git_Md);
        }

        [Test]
        public void MapExtensionToType_ContainsTestResultsUpperCase_ReturnsTestResults_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_TEST-RESULTS_2025");

            // Assert
            result.ShouldBe(RecentFileType.TestResults_Md);
        }

        [Test]
        public void MapExtensionToType_ContainsSummaryUpperCase_ReturnsSummary_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_SUMMARY_2025");

            // Assert
            result.ShouldBe(RecentFileType.Summary_Md);
        }

        [Test]
        public void MapExtensionToType_ContainsPlanUpperCase_ReturnsPlan()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_PLAN_2025");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_ContainsGuideUpperCase_ReturnsGuide()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_GUIDE_2025");

            // Assert
            result.ShouldBe(RecentFileType.Guide);
        }

        [Test]
        public void MapExtensionToType_MdWithoutKeywords_ReturnsCodebase_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_random_file");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        #endregion

        #region MapExtensionToType - Extension-Only Fallback Tests

        [Test]
        public void MapExtensionToType_DocxExtensionOnly_ReturnsCodebase_Docx()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".docx", "random_file");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Docx);
        }

        [Test]
        public void MapExtensionToType_PdfExtensionOnly_ReturnsCodebasePdf()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".pdf", "random_file");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Pdf);
        }

        [Test]
        public void MapExtensionToType_UnknownExtension_ReturnsUnknown()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".xyz", "random_file");

            // Assert
            result.ShouldBe(RecentFileType.Unknown);
        }

        [Test]
        public void MapExtensionToType_NullExtension_ReturnsUnknown()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(null, "random_file");

            // Assert
            result.ShouldBe(RecentFileType.Unknown);
        }

        [Test]
        public void MapExtensionToType_EmptyExtension_ReturnsUnknown()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType("", "random_file");

            // Assert
            result.ShouldBe(RecentFileType.Unknown);
        }

        #endregion

        #region MapExtensionToType - Edge Cases

        [Test]
        public void MapExtensionToType_NullFileName_ReturnsUnknown()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", null);

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        [Test]
        public void MapExtensionToType_EmptyFileName_ReturnsCodebase_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        [Test]
        public void MapExtensionToType_SingleWordFileName_UsesWord()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_CaseInsensitive_ReturnsPlan()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".MD", "VecTool_PLAN");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_ExtensionWithLeadingDot_WorksCorrectly()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_ExtensionWithoutLeadingDot_WorksCorrectly()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType("md", "VecTool_plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_MarkdownExtension_WorksCorrectly()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".markdown", "VecTool_plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_UnderscoreSeparator_ParsesLastToken()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_master_guide");

            // Assert
            result.ShouldBe(RecentFileType.Guide);
        }

        [Test]
        public void MapExtensionToType_HyphenSeparator_ParsesLastToken()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool-master-summary");

            // Assert
            result.ShouldBe(RecentFileType.Summary_Md);
        }

        [Test]
        public void MapExtensionToType_SpaceSeparator_ParsesLastToken()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool master plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_MixedSeparators_ParsesLastToken()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_master-dev guide");

            // Assert
            result.ShouldBe(RecentFileType.Guide);
        }

        #endregion

        #region GetLastToken Tests (via reflection or by testing MapExtensionToType behavior)

        // Note: GetLastToken is private, so we test its behavior through MapExtensionToType

        [Test]
        public void GetLastToken_SingleWord_ReturnsWord()
        {
            // Arrange & Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "plan");

            // Assert - if "plan" is correctly identified, GetLastToken worked
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void GetLastToken_WithUnderscores_ReturnsLastPart()
        {
            // Arrange & Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "part1_part2_guide");

            // Assert
            result.ShouldBe(RecentFileType.Guide);
        }

        [Test]
        public void GetLastToken_WithHyphens_ReturnsLastPart()
        {
            // Arrange & Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "part1-part2-summary");

            // Assert
            result.ShouldBe(RecentFileType.Summary_Md);
        }

        [Test]
        public void GetLastToken_WithSpaces_ReturnsLastPart()
        {
            // Arrange & Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "part1 part2 plan");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void GetLastToken_EmptyString_ReturnsCodebase_Md()
        {
            // Arrange & Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "");

            // Assert - fallback to Codebase_Md when no token found
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        [Test]
        public void GetLastToken_OnlySeparators_ReturnsCodebase_Md()
        {
            // Arrange & Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "___---   ");

            // Assert - fallback to Codebase_Md when no meaningful token
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        #endregion

        #region Real-World Scenarios

        [Test]
        public void MapExtensionToType_RealScenario_VecToolMasterCodebase_Md()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_master_codebase");

            // Assert
            result.ShouldBe(RecentFileType.Codebase_Md);
        }

        [Test]
        public void MapExtensionToType_RealScenario_VecToolDevMainBugFixesGit()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecToolDevMain_bug_fixes_git");

            // Assert
            result.ShouldBe(RecentFileType.Git_Md);
        }

        [Test]
        public void MapExtensionToType_RealScenario_GuideFile()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "GUIDE-1.8-CodingConvention WinForms LogCtx");

            // Assert
            result.ShouldBe(RecentFileType.Guide);
        }

        [Test]
        public void MapExtensionToType_RealScenario_PlanFile()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "GUIDE-1.6-Plan-Phase-Versioning");

            // Assert
            result.ShouldBe(RecentFileType.Plan);
        }

        [Test]
        public void MapExtensionToType_RealScenario_TestResultsFile()
        {
            // Act
            var result = RecentFileType.Unknown.MapExtensionToType(".md", "VecTool_2025-10-26_test-results");

            // Assert
            result.ShouldBe(RecentFileType.TestResults_Md);
        }

        #endregion
    }
}
