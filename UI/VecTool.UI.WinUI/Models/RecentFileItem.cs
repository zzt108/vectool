// ✅ NEW — File: UI/VecTool.UI.WinUI/Models/RecentFileItem.cs
using System;

namespace VecTool.UI.WinUI
{
    // Public so XAML compiler can resolve it for x:DataType
    public sealed class RecentFileItem
    {
        public string FileName { get; }
        public string Path { get; }
        public string? LinkedStoreName { get; }

        public RecentFileItem(string path, string? linkedStoreName)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            FileName = System.IO.Path.GetFileName(path);
            LinkedStoreName = linkedStoreName;
        }
    }
}
