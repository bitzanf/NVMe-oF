using ManagementApp.Models;
using System;
using System.Windows.Input;

namespace ManagementApp.Helpers;

internal class DiskCommandEventHandler(Action<DiskConnectionModel> action) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is not DiskConnectionModel model) return;

        action(model);
    }
}
