using System;
using System.Windows.Input;

namespace VecTool.Studio.Commands;

/// <summary>
/// Simple ICommand implementation for Phase 2 command plumbing.
/// Wraps an Action delegate without async support (use RelayCommandAsync in Phase 3).
/// </summary>
internal sealed class SimpleCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public SimpleCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}