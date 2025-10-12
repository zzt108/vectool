// Path: UI/VecTool.UI.WinUI/About/AboutPage.xaml.cs
// Phase 3.1: AboutPage with IVersionProvider data binding via AboutVersionAdapter

using Microsoft.UI.Xaml.Controls;
using VecTool.Core.Versioning;

namespace VecTool.UI.WinUI.About
{
    /// <summary>
    /// About page displaying application version information via data binding.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// var versionProvider = new AssemblyVersionProvider();
    /// var aboutPage = new AboutPage(versionProvider);
    /// // Host in ContentDialog or navigate to in Frame
    /// </code>
    /// </remarks>
    public sealed partial class AboutPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutPage"/> class.
        /// </summary>
        /// <param name="versionProvider">The version provider containing application metadata.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="versionProvider"/> is null.</exception>
        public AboutPage(IVersionProvider versionProvider)
        {
            this.InitializeComponent();

            // Wrap IVersionProvider in adapter for XAML data binding
            // (WinUI 3 requires public properties, not interface members)
            this.DataContext = new AboutVersionAdapter(versionProvider);
        }
    }
}
