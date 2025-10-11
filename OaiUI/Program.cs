using System.Linq;
using System.Windows.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLogShared;

namespace VecTool.UI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Initialize robust, non-throwing logging
            LoggingBootstrap.Initialize();

            Application.ThreadException += (sender, args) =>
                LogManager.GetCurrentClassLogger().Error(args.Exception, "Unhandled UI thread exception.");
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                LogManager.GetCurrentClassLogger().Fatal(ex, "Unhandled domain exception.");
            };

            ApplicationConfiguration.Initialize();

            var mainForm = CreateMainFormOrFallback();

            // NEW CODE: apply window icon at runtime even if designer didn’t set it
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Vectool.ico");
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
                LogManager.GetCurrentClassLogger().Error(ex, "Failed to create MainForm.");
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

    /// <summary>
    /// Fail-safe logging bootstrap living at app layer but powered by LogCtx (NLogShared.CtxLogger).
    /// - Tries XML/JSON configs in AppContext.BaseDirectory and common subfolders.
    /// - Falls back to a minimal in-memory NLog configuration if config load fails.
    /// - Never throws; guarantees logging won't break application startup.
    /// </summary>
    internal static class LoggingBootstrap
    {
        public static void Initialize(string? xmlName = "NLog.config", string? jsonName = "NLog.json")
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;

                // Candidate config locations relative to the runtime base directory.
                var candidates = new[]
                {
                    Path.Combine(baseDir, xmlName ?? "NLog.config"),
                    Path.Combine(baseDir, jsonName ?? "NLog.json"),
                    Path.Combine(baseDir, "Config", "LogConfig.xml"), // per repo README
                    Path.Combine(baseDir, "Config", "NLog.config"),
                    Path.Combine(baseDir, "Config", "NLog.json")
                };

                // Try XML/JSON via LogCtx's CtxLogger.
                foreach (var path in candidates)
                {
                    if (!File.Exists(path))
                        continue;

                    var ctx = new CtxLogger();
                    var isJson = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
                    var ok = isJson ? ctx.ConfigureJson(path) : ctx.ConfigureXml(path);
                    if (ok)
                        return;
                }

                // If none worked, set a minimal in-memory NLog config.
                ApplyMinimalFallback(baseDir);
            }
            catch
            {
                // Absolutely never throw from logging bootstrap.
                ApplyMinimalFallback(AppContext.BaseDirectory);
            }
        }

        private static void ApplyMinimalFallback(string baseDir)
        {
            try
            {
                var config = new LoggingConfiguration();

                var console = new ConsoleTarget("console")
                {
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
                };

                var logsDir = Path.Combine(baseDir, "logs");
                try { Directory.CreateDirectory(logsDir); } catch { /* ignore */ }

                var file = new FileTarget("file")
                {
                    FileName = Path.Combine(logsDir, "app.log"),
                    ArchiveFileName = Path.Combine(logsDir, "app.{#}.log"),
                    ArchiveAboveSize = 5_000_000,
                    MaxArchiveFiles = 5,
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
                };

                config.AddTarget(console);
                config.AddTarget(file);
                config.AddRuleForAllLevels(console);
                config.AddRuleForAllLevels(file);

                LogManager.Configuration = config;
            }
            catch
            {
                // If even this fails, leave NLog unconfigured (no-op) but do not throw.
            }
        }
    }
}
