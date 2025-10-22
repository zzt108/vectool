using NLogShared;
using System.Linq;
using System.Windows.Forms;
using Vectool.UI.Versioning;

namespace Vectool.OaiUI
{
    internal static class Program
    {
        public static NLogShared.CtxLogger Log { get; private set; } = new CtxLogger("Config/LogConfig.xml");

        [STAThread]
        private static void Main()
        {
                      
            Log.Info("Starting VecTool application.");
            Application.ThreadException += (sender, args) =>
                Log.Error(args.Exception, "Unhandled UI thread exception.");
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Log.Fatal(ex, "Unhandled domain exception.");
            };

            ApplicationConfiguration.Initialize();

            var versionProvider = new AssemblyVersionProvider();
            var mainForm = new MainForm(versionProvider);

            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Media", "icon.ico");
                if (File.Exists(iconPath))
                {
                    using var fs = File.OpenRead(iconPath);
                    mainForm.Icon = new System.Drawing.Icon(fs);
                }
            }
            catch
            {
                // Do not block startup on icon failures
            }

            // NEW: Visibility safeguards to ensure the main window is actually visible
          if (mainForm.WindowState == FormWindowState.Minimized)
              mainForm.WindowState = FormWindowState.Normal;

          mainForm.ShowInTaskbar = true;

          if (mainForm.Width < 400 || mainForm.Height < 300)
              mainForm.Size = new System.Drawing.Size(
                  Math.Max(mainForm.Width, 800),
                  Math.Max(mainForm.Height, 600));


            Application.Run(mainForm);
        }

        private static Form CreateMainFormOrFallback()
        {
            try
            {

                var candidateTypes = typeof(Program).Assembly
                    .GetTypes()
                    .Where(t => typeof(Form).IsAssignableFrom(t) && string.Equals(t.Name, "MainForm", StringComparison.Ordinal))
                    .ToList();

                var preferred = candidateTypes
                    .FirstOrDefault(t => string.Equals(t.FullName, "Vectool.OaiUI.MainForm", StringComparison.Ordinal));

                var formType = preferred ?? candidateTypes.FirstOrDefault();

                if (formType != null)
                {
                    new CtxLogger().Info($"Launching MainForm type: {formType.FullName}");
                    var instance = Activator.CreateInstance(formType) as Form;
                    if (instance != null) return instance;
                }

                MessageBox.Show("MainForm type was not found. Ensure the UI project compiles and the MainForm class exists.",
                                "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new Form
                {
                    Text = "VecTool - Minimal Shell (MainForm not found)",
                    StartPosition = FormStartPosition.CenterScreen,
                    Width = 800,
                    Height = 600
                };
            }
            catch (Exception ex)
            {
                new CtxLogger().Error(ex, "Failed to create MainForm.");
                return new Form
                {
                    Text = "VecTool - Startup Error (Fallback Shell)",
                    StartPosition = FormStartPosition.CenterScreen,
                    Width = 800,
                    Height = 600
                };
            }
        }
    }

}
