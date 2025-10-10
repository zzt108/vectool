// ✅ FULL FILE VERSION
// Location: OaiUI/MainForm.GitHelpers.cs
// Description: Git helpers for MainForm to resolve current branch name safely.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VecTool.Core;

namespace Vectool.UI
{
    public partial class MainForm
    {
        // Returns a sanitized current branch name if a repo is detected, otherwise "unknown".
        // Uses RepoLocator to pick an appropriate working directory from selected folders.
        private async Task<string> GetCurrentBranchNameAsync()
        {
            try
            {
                // Prefer a repo root from the currently selected folders; fall back to base directory.
                var preferred = RepoLocator.ResolvePreferredWorkingDirectory(selectedFolders);
                var workingDir = string.IsNullOrWhiteSpace(preferred) ? AppContext.BaseDirectory : preferred;

                // Not a git repo? Bail out fast.
                if (!GitRunner.IsGitRepository(workingDir))
                    return "unknown";

                // Ask git for the current branch; GitRunner normalizes error paths to "unknown".
                var runner = new GitRunner(workingDir);
                var raw = await runner.GetCurrentBranchAsync().ConfigureAwait(true);

                // Sanitize for file-stem usage to avoid invalid characters in generated filenames.
                var safe = SanitizeFileName(raw ?? "unknown", "-");
                return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
            }
            catch
            {
                // Never let branch resolution break UI flows.
                return "unknown";
            }
        }
    }
}
