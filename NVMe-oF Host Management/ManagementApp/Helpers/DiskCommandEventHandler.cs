using ManagementApp.Models;
using System;
using System.Windows.Input;

namespace ManagementApp.Helpers;

/// <summary>
/// UI Command using a DiskConnectionModel as a parameter passed to the given callback
/// </summary>
/// <param name="action"></param>
internal partial class DiskCommandEventHandler(Action<DiskConnectionModel> action) : ICommand
{
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is not DiskConnectionModel model) return;

        action(model);
    }
}
