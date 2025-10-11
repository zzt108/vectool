// NEW FILE
// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VecTool.Handlers;

namespace VecTool.UI.WinUI.Infrastructure
{
    /// <summary>
    /// WinUI 3 implementation of IUserInterface that marshals all UI updates to the dispatcher queue.
    /// Mirrors WinFormsUserInterface behavior with DispatcherQueue instead of Control.Invoke.
    /// </summary>
    public sealed class WinUiUserInterface : IUserInterface
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly TextBlock statusLabel;
        private readonly ProgressBar progressBar;
        private readonly DispatcherQueue dispatcher;

        public int TotalWork { get; set; }

        public WinUiUserInterface(TextBlock statusLabel, ProgressBar progressBar, DispatcherQueue dispatcher)
        {
            this.statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            this.progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            // Initialize safe defaults on the UI thread
            InvokeOnUi(() =>
            {
                statusLabel.Text = "Ready";
                progressBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                progressBar.Minimum = 0;
                progressBar.Maximum = 1;
                progressBar.Value = 0;
                progressBar.IsIndeterminate = false;
            });

            Log.Info("WinUiUserInterface initialized with dispatcher={DispatcherId}", dispatcher.GetHashCode());
        }

        public void WorkStart(string workText, List<string> selectedFolders)
        {
            TotalWork = GetTotalFolders(selectedFolders);
            InvokeOnUi(() =>
            {
                statusLabel.Text = workText;
                progressBar.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                progressBar.Minimum = 0;
                progressBar.Maximum = Math.Max(1, TotalWork);
                progressBar.Value = 0;
                progressBar.IsIndeterminate = false;
            });

            Log.Info("Work started: {WorkText} with TotalWork={TotalWork}", workText, TotalWork);
        }

        public void WorkFinish()
        {
            InvokeOnUi(() =>
            {
                progressBar.Value = 0;
                progressBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                statusLabel.Text = "Finished";
            });

            Log.Info("Work finished");
        }

        public void ShowMessage(string message, string title = "Information", MessageType type = MessageType.Information)
        {
            InvokeOnUi(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = statusLabel.XamlRoot // Ensure dialog is parented correctly
                };

                await dialog.ShowAsync();
            });

            Log.Info("ShowMessage: Title={Title}, Type={Type}, Message={Message}", title, type, message);
        }

        public void UpdateProgress(int current)
        {
            var maximum = Math.Max(1, TotalWork);
            if (current < 0) current = 0;
            if (current > maximum) current = maximum;

            InvokeOnUi(() =>
            {
                progressBar.Maximum = maximum;
                progressBar.Value = current;
            });

            Log.Debug("UpdateProgress: {Current}/{Maximum}", current, maximum);
        }

        public void UpdateStatus(string statusText)
        {
            InvokeOnUi(() =>
            {
                statusLabel.Text = statusText;
            });

            Log.Debug("UpdateStatus: {StatusText}", statusText);
        }

        private void InvokeOnUi(Action action)
        {
            if (dispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                dispatcher.TryEnqueue(() => action());
            }
        }

        private static int GetTotalFolders(List<string> selectedFolders)
        {
            if (selectedFolders == null || selectedFolders.Count == 0)
                return 1;

            var total = 0;
            foreach (var folder in selectedFolders)
            {
                try
                {
                    // Count this folder + subfolders to give a bounded progress scale
                    total += 1 + Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).Length;
                }
                catch
                {
                    total += 1;
                }
            }

            return Math.Max(1, total);
        }
    }
}
