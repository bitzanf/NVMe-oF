﻿using System;
using Windows.ApplicationModel.Resources;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;

namespace ManagementApp.Helpers;

public static class NotificationHelper
{
    public static void ShowChangeSaveStatus(bool success)
    {
        var loader = ResourceLoader.GetForViewIndependentUse();
        if (loader == null) return;

        Notification notification = success
            ? new()
            {
                Title = loader.GetString("ChangesSavedNotification_Success"),
                Duration = TimeSpan.FromSeconds(5),
                Severity = InfoBarSeverity.Success
            }
            : new()
            {
                Title = loader.GetString("ChangesSavedNotification_Error"),
                Message = loader.GetString("ChangesSavedNotification_ErrorMessage"),
                Duration = TimeSpan.FromSeconds(15),
                Severity = InfoBarSeverity.Error
            };

        MainWindow.Instance?.ShowNotification(notification);
    }
}