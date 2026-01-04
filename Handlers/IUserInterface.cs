namespace VecTool.Handlers;

public enum MessageType
{
    Information,
    Warning,
    LogError
}

public interface IUserInterface
{
    int TotalWork { get; set; }
    void ShowMessage(string message, string title = "Information", MessageType type = MessageType.Information);
    void UpdateProgress(int current);
    void UpdateStatus(string statusText);
    void WorkStart(string workText, IEnumerable<string> selectedFolders);
    void WorkFinish();
}