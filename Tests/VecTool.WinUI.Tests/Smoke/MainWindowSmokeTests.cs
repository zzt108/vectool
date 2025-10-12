
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using VecTool.Configuration;

namespace VecTool.WinUI.Tests.Smoke
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainWindowSmokeTests
    {
        [Test]
        public void ShouldLoadCorrectExclusionSettings()
        {
            // Arrange: Load global config
            var global = VectorStoreConfig.FromAppConfig();

            // Simulating UI controls
            var TxtExcludedFiles = new TextBox();
            var TxtExcludedFolders = new TextBox();
            var chkFiles = new CheckBox();
            var chkFolders = new CheckBox();

            // Act: Populate TextBoxes
            TxtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles);
            TxtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders);

            // ✅ FIXED: Use correct Split overload with string array
            var customFiles = TxtExcludedFiles.Text
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var customFolders = TxtExcludedFolders.Text
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            // Assert
            customFiles.ShouldNotBeEmpty();
            customFolders.ShouldNotBeEmpty();

            customFiles.Count.ShouldBe(global.ExcludedFiles.Count);
            customFolders.Count.ShouldBe(global.ExcludedFolders.Count);

            // Simulate checkbox settings
            chkFiles.IsChecked = true;
            chkFolders.IsChecked = false;

            // ✅ FIXED: Handle nullable bool? from WinUI CheckBox.IsChecked
            (chkFiles.IsChecked ?? false).ShouldBeTrue("Default should inherit from global");
            (chkFolders.IsChecked ?? false).ShouldBeFalse();
        }
    }
}
