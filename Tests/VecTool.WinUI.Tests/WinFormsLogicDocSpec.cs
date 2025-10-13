using System.IO;
using NUnit.Framework;
using Shouldly;

namespace VecTool.WinUI.Tests.Architecture;

/// <summary>
/// Architecture guardrail test to ensure the WinForms business logic documentation
/// is created and complete before proceeding with code removal in Phase 4.1.3 b2.2.
/// This test prevents accidental deletion of legacy code without its behavior
/// being preserved in the reference documentation.
/// </summary>
[TestFixture]
public class WinFormsLogicDocSpec
{
    // Relative path from the test execution directory to the documentation artifact.
    // Assumes the test runner is executed from a context where this path is resolvable.
    private const string DocPath = "../../../../Docs/WinForms-Business-Logic-Reference.md";

    /// <summary>
    /// Validates that the "WinForms-Business-Logic-Reference.md" file exists and
    /// contains the four mandatory top-level sections required by the migration plan.
    /// This check ensures that the core pattern families have been addressed before
    /// the source WinForms code is deleted from the solution.
    /// </summary>
    [Test(Description = "Ensures WinForms logic doc exists and has all required sections for b2.1 completion.")]
    public void ShouldContainRequiredSections()
    {
        // 1. Arrange & Act: Check for file existence.
        // The assertion message clarifies the dependency: this doc MUST exist before starting the next phase.
        File.Exists(DocPath).ShouldBeTrue(
            $"The documentation file at '{Path.GetFullPath(DocPath)}' must exist before starting Phase b2.2 (WinForms Project Removal). " +
            "If this test fails, run the b2.1 documentation step first.");

        // 2. Read the entire content of the Markdown file.
        var text = File.ReadAllText(DocPath);

        // 3. Assert: Verify that each of the four required sections is present.
        // These headings correspond to the critical logic patterns identified for preservation.
        text.ShouldContain("## Vector Store Selection",Case.Insensitive, "Section missing: Vector Store Selection");
        text.ShouldContain("## File Operations & Handlers", Case.Insensitive, "Section missing: File Operations & Handlers");
        text.ShouldContain("## Recent Files Management", Case.Insensitive, "Section missing: Recent Files Management");
        text.ShouldContain("## Settings Persistence", Case.Insensitive, "Section missing: Settings Persistence");
    }
}
