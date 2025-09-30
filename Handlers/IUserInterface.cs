namespace VecTool.Handlers;

/// <summary>
/// Interface for UI feedback during handler operations.
/// </summary>
public interface IUserInterface
{
    /// <summary>
    /// Updates the status message displayed to the user.
    /// </summary>
    void UpdateStatus(string message);

    /// <summary>
    /// Reports progress (0.0 to 1.0).
    /// </summary>
    void UpdateProgress(double progress);

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    void ShowError(string message);
}
