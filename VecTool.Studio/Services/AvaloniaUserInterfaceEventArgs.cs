using VecTool.Handlers;

namespace VecTool.Studio.Services
{
    /// <summary>
    /// Event args for StatusChanged event.
    /// Phase 2 Step 3: Event-driven status updates.
    /// </summary>
    public class UIStatusChangedEventArgs : EventArgs
    {
        public string StatusText { get; }

        public UIStatusChangedEventArgs(string statusText)
        {
            StatusText = statusText;
        }
    }

    /// <summary>
    /// Event args for ProgressChanged event.
    /// Phase 2 Step 3: Event-driven progress updates.
    /// Renamed to avoid conflict with System.ComponentModel.ProgressChangedEventArgs.
    /// </summary>
    public class UIProgressChangedEventArgs : EventArgs
    {
        public int Current { get; }
        public int Maximum { get; }

        public UIProgressChangedEventArgs(int current, int maximum)
        {
            Current = current;
            Maximum = maximum;
        }
    }

    /// <summary>
    /// Event args for MessageShown event.
    /// Phase 2 Step 3/6: Event-driven message dialogs.
    /// </summary>
    public class UIMessageShownEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public MessageType Type { get; }

        public UIMessageShownEventArgs(string title, string message, MessageType type)
        {
            Title = title;
            Message = message;
            Type = type;
        }
    }
}