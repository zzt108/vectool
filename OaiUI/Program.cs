using Vectool.OaiUI;

namespace oai
{
    // TODO: refactoring: Reimplement version numbering system from last Vectool version before refactor
    // TODO: Convert to  single file could do get git changes automatically paralell.
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}