using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace ManagementApp.Converters;

/// <summary>
/// Catches any exception and displays it (along with a stack trace) in the main window as an error notification
/// </summary>
public static class ExceptionToNotificationConverter
{
    public static T WrapExceptions<T>(Func<T> callback)
    {
        try
        {
            return callback();
        }
        catch (Exception e)
        {
            ShowNotification(e);
            return default!;
        }
    }

    public static void WrapExceptions(Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception e)
        {
            ShowNotification(e);
        }
    }

    public static async Task<T> WrapExceptionsAsync<T>(Func<Task<T>> callback)
    {
        try
        {
            return await callback();
        }
        catch (Exception e)
        {
            ShowNotification(e);
            return default!;
        }
    }

    public static async Task WrapExceptionsAsync(Func<Task> callback)
    {
        try
        {
            await callback();
        }
        catch (Exception e)
        {
            ShowNotification(e);
        }
    }

    /// <summary>
    /// Create and display an error notification for the given exception
    /// </summary>
    /// <param name="ex"></param>
    private static void ShowNotification(Exception ex)
    {
        if (MainWindow.Instance == null) return;

        var loader = ResourceLoader.GetForViewIndependentUse();
        var notification = new Notification
        {
            Severity = InfoBarSeverity.Error,
            Title = loader.GetString("ErrorNotification"),
            Message = ex.ToString()
        };

        MainWindow.Instance?.ShowNotification(notification);
    }
}