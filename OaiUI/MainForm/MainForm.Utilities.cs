// ✅ FULL FILE VERSION
// File: OaiUI/MainForm.Configuration.cs

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm partial: Utility and helper methods.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Sanitizes a filename by replacing invalid characters with a replacement character.
        /// </summary>
        /// <param name="input">Input string to sanitize.</param>
        /// <param name="replacement">Replacement character (default: '_').</param>
        /// <returns>Sanitized filename, or default if input is invalid.</returns>
        private static string SanitizeFileName(string input, string replacement = "_")
        {
            // ✅ Guard: ensure a non-empty replacement character
            var replChar = string.IsNullOrEmpty(replacement) ? '_' : replacement[0];

            // ✅ Replace invalid filename chars with the replacement char
            if (string.IsNullOrEmpty(input))
                return default;

            var sanitized = input;
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var ch in invalidChars)
            {
                sanitized = sanitized.Replace(ch, replChar);
            }

            // ✅ Normalize whitespace to replacement char
            foreach (var ch in new[] { '\r', '\n', '\t' })
            {
                sanitized = sanitized.Replace(ch, replChar);
            }

            // ✅ Collapse consecutive replacement chars (safely, no infinite loop)
            var doubleRepl = new string(replChar, 2);
            var singleRepl = new string(replChar, 1);
            while (sanitized.Contains(doubleRepl, StringComparison.Ordinal))
            {
                sanitized = sanitized.Replace(doubleRepl, singleRepl, StringComparison.Ordinal);
            }

            // ✅ Trim leading/trailing replacement, dots, and spaces
            sanitized = sanitized.Trim(replChar, '.', ' ');

            return string.IsNullOrWhiteSpace(sanitized) ? default : sanitized;
        }
    }
}
