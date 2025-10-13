// ✅ FULL FILE VERSION
using System;
using System.IO;
using NUnit.Framework;
using Shouldly;

namespace VecTool.WinUI.Tests.Architecture;

/// <summary>
/// Ensures the WinForms business logic documentation, a deliverable of phase b2.1,
/// is complete before allowing the WinForms project removal in phase b2.2.
/// </summary>
[TestFixture]
[Category("Architecture")]
public class WinFormsLogicDocSpec
{
    /// <summary>
    /// ✅ NEW: Robustly resolves the path to the WinForms logic documentation.
    /// It checks the test output directory first, then walks up to find the solution root.
    /// </summary>
    private static string ResolveDocPath()
    {
        // 1) Prefer copied artifact under bin\Docs for self-contained CI runs
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var inBin = Path.Combine(baseDir, "Docs", "WinForms-Business-Logic-Reference.md");
        if (File.Exists(inBin)) return inBin;

        // 2) Walk up to solution root (VecTool.sln), then resolve Docs/...
        // This is a fallback for local dev environments where the file may not be copied.
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "VecTool.sln");
            if (File.Exists(sln))
                return Path.Combine(dir.FullName, "Docs", "WinForms-Business-Logic-Reference.md");
            dir = dir.Parent;
        }

        // 3) Fallback to the expected bin path, which will fail the assert with a clear message
        return inBin;
    }

    /// <summary>
    /// 🔄 MODIFY: Test now uses the dynamic path resolver to find the document.
    /// </summary>
    [Test]
    //[TestDescription("Ensures WinForms logic doc exists and has all required sections for b2.1 completion.")]
    public void ShouldContainRequiredSections()
    {
        // Use the robust path resolution logic
        var docPath = ResolveDocPath();

        // Assert existence with an actionable, clear message including the resolved path
        File.Exists(docPath).ShouldBeTrue(
            $"The documentation file at '{Path.GetFullPath(docPath)}' must exist before starting Phase b2.2 (WinForms Project Removal). " +
            $"If this test fails, ensure the file is present and the test project copies it to the output directory."
        );

        var text = File.ReadAllText(docPath);

        // Assert that all four required headings from the plan are present
        text.ShouldContain("Vector Store Selection", Case.Insensitive, "Section missing: Vector Store Selection");
        text.ShouldContain("File Operations \\& Handlers", Case.Insensitive, "Section missing: File Operations & Handlers");
        text.ShouldContain("Recent Files Management", Case.Insensitive, "Section missing: Recent Files Management");
        text.ShouldContain("Settings Persistence", Case.Insensitive, "Section missing: Settings Persistence");
    }
}
