// UI/VecTool.UI.WinUI/MainWindow.xaml.cs
// WinUI 3 migration-compliant MainWindow with NLog, DispatcherQueue, and VisualTreeHelper

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VecTool.UI.WinUI;

public sealed partial class MainWindow : Window
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    public MainWindow()
    {
        InitializeComponent();
        Log.Info("MainWindow initialized");
    }

    #region Lifecycle & Initialization

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Info("Window loaded, content tree ready");
        UpdateUIState();
    }

    #endregion

    #region Event Handlers

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Info("Save button clicked");

        // Example: UI marshaling with DispatcherQueue
        var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        dq.TryEnqueue(() =>
        {
            Log.Info("Save operation dispatched to UI thread");
            // Perform save logic here
        });
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Info("Cancel button clicked");
        Close();
    }

    #endregion

    #region Drag & Drop Handlers

    private void ContentPanel_DragOver(object sender, DragEventArgs e)
    {
        // 🚫 WPF pattern: e.Effects = DragDropEffects.Copy
        // ✅ WinUI 3 pattern:
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop files here";
        e.Handled = true;
    }

    private async void ContentPanel_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var paths = items.Select(i => i.Path).ToList();

                Log.Info("Files dropped: {Count} items", paths.Count);

                foreach (var path in paths)
                {
                    Log.Info("Processing dropped file: {Path}", path);
                }

                ProcessDroppedFiles(paths);
            }
        }
        catch (Exception ex)
        {
            var evt = new LogEventInfo(LogLevel.Error, Log.Name, "Drop operation failed");
            evt.Exception = ex;
            Log.Log(evt);

            // ✅ WinUI 3 dialog pattern with XamlRoot
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = "Failed to process dropped files.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    #endregion

    #region UI State Management

    private void UpdateUIState()
    {
        if (BtnSaveVsSettings != null)
        {
            BtnSaveVsSettings.IsEnabled = true;
        }

        Log.Info("UI state updated");
    }

    private void RefreshVisualTree()
    {
        // ✅ WinUI 3: VisualTreeHelper from Microsoft.UI.Xaml.Media
        if (this.Content is DependencyObject root)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(root);
            Log.Info("Visual tree has {ChildCount} children", childCount);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                Log.Info("Child {Index}: {Type}", i, child.GetType().Name);
            }
        }
    }

    #endregion

    #region Business Logic Orchestration

    private void ProcessDroppedFiles(List<string> paths)
    {
        Log.Info("Processing {Count} dropped files", paths.Count);

        // TODO: Wire to actual file processing service
        foreach (var path in paths)
        {
            Log.Info("File queued for processing: {Path}", path);
        }
    }

    #endregion

    #region Helpers

    private void NavigateToPanel(Type panelType)
    {
        Log.Info("Navigating to panel: {PanelType}", panelType.Name);

        // Example: WinUI 3 Frame navigation
        // ContentFrame.Navigate(panelType);
    }

    #endregion
}
