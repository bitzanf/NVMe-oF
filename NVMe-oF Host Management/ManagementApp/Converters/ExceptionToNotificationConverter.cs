using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace ManagementApp.Converters;

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

        MainWindow.Instance.ShowNotification(notification);
    }
}