// FULL FILE VERSION
// Path: src/UI/VecTool.UI.WinUI/MainWindow.xaml.cs

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            Log.Info("MainWindow constructed at {TimestampUtc}", DateTime.UtcNow);
        }
    }
}
