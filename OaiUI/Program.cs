using System.Linq;
using System.Windows.Forms;
//using NLog;
//using NLog.Config;
//using NLog.Targets;
using NLogShared;

namespace Vectool.UI
{
    internal static class Program
    {

        [STAThread]
        private static void Main()
        {
            
            using var log = new CtxLogger();
            
            log.Info("Starting VecTool application.");
            Application.ThreadException += (sender, args) =>
                new CtxLogger().Error(args.Exception, "Unhandled UI thread exception.");
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                new CtxLogger().Fatal(ex, "Unhandled domain exception.");
            };

            ApplicationConfiguration.Initialize();

            var mainForm = CreateMainFormOrFallback();

            // NEW CODE: apply window icon at runtime even if designer didn’t set it
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

            Application.Run(mainForm);
        }

        private static Form CreateMainFormOrFallback()
        {
            try
            {
                var formType = typeof(Program).Assembly
                    .GetTypes()
                    .FirstOrDefault(t => typeof(Form).IsAssignableFrom(t) && string.Equals(t.Name, "MainForm", StringComparison.Ordinal));
                if (formType != null)
                {
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
