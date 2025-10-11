// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml.Controls;
using VecTool.Core.Versioning;

namespace VecTool.UI.WinUI.About
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage(IVersionProvider versions)
        {
            this.InitializeComponent();
            DataContext = new AboutVersionAdapter(versions);
        }
    }
}
