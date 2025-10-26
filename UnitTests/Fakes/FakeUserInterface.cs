// ✅ FULL FILE VERSION
using System.Collections.Generic;
using VecTool.Handlers;

namespace UnitTests.Fakes
{
    public sealed class FakeUserInterface : IUserInterface
    {
        public string? LastMessage { get; private set; }
        public string? LastTitle { get; private set; }
        public MessageType? LastType { get; private set; }
        public string? LastStatus { get; private set; }
        public IReadOnlyList<string>? LastFolders { get; private set; }
        public int LastProgress { get; private set; }

        // 🔄 MODIFY: Public setter to match interface (WinForms version exposes public set)
        public int TotalWork { get; set; }

        public void ShowMessage(string message, string title, MessageType type)
        {
            LastMessage = message;
            LastTitle = title;
            LastType = type;
        }

        public void WorkStart(string status, IEnumerable<string> selectedFolders)
        {
            LastStatus = status;
            LastFolders = selectedFolders.ToList();
            TotalWork = LastFolders?.Count ?? 0;
        }

        public void WorkFinish()
        {
            // no-op
        }

        public void UpdateStatus(string status)
        {
            LastStatus = status;
        }

        public void UpdateProgress(int percent)
        {
            LastProgress = percent;
        }
    }
}
