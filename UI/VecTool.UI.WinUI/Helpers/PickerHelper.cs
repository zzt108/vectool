// ✅ NEW: WinUI picker helper with HWND initialization
// File: UI/VecTool.UI.WinUI/Helpers/PickerHelper.cs
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using NLog;

namespace VecTool.UI.WinUI.Helpers
{
    /// <summary>
    /// Helper for WinUI 3 file/folder pickers requiring HWND initialization.
    /// </summary>
    public static class PickerHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Shows a folder picker dialog and returns the selected folder, or null if canceled.
        /// </summary>
        /// <param name="window">Parent window for HWND retrieval.</param>
        /// <param name="title">Dialog title (optional).</param>
        /// <returns>Selected StorageFolder or null.</returns>
        public static async Task<StorageFolder?> PickFolderAsync(Window window, string? title = null)
        {
            try
            {
                var picker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };
                picker.FileTypeFilter.Add("*"); // Required for FolderPicker

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    Log.Info("Folder selected: {Path}", folder.Path);
                    return folder;
                }

                Log.Debug("Folder picker canceled by user");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Folder picker failed");
                return null;
            }
        }

        /// <summary>
        /// Shows a file save picker dialog and returns the selected file, or null if canceled.
        /// </summary>
        /// <param name="window">Parent window for HWND retrieval.</param>
        /// <param name="suggestedFileName">Default filename.</param>
        /// <param name="fileTypeChoices">Dictionary of display names to file extensions.</param>
        /// <returns>Selected StorageFile or null.</returns>
        public static async Task<StorageFile?> PickSaveFileAsync(
            Window window,
            string suggestedFileName,
            (string displayName, string[] extensions)[] fileTypeChoices)
        {
            try
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = suggestedFileName
                };

                foreach (var (displayName, extensions) in fileTypeChoices)
                {
                    picker.FileTypeChoices.Add(displayName, extensions);
                }

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    Log.Info("Save file selected: {Path}", file.Path);
                    return file;
                }

                Log.Debug("Save picker canceled by user");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Save picker failed");
                return null;
            }
        }
    }
}
