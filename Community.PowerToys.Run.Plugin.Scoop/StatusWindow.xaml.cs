// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Common.UI;
using Microsoft.Xaml.Behaviors.Core;
using Wpf.Ui.Appearance;

namespace Community.PowerToys.Run.Plugin.Scoop;

public partial class StatusWindow : INotifyPropertyChanged
{
    public Scoop.Package Package { get; }

    public string ActionPrefix { get; }

    public string VersionSuffix { get; }

    public Uri IconUri { get; }

    public double Progress
    {
        set
        {
            StatusProgressBar.Value = value;
            StatusProgressBar.IsIndeterminate = value < 0;
        }
    }

    public bool ProgressBarVisible
    {
        set => StatusProgressBar.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public Brush ProgressBarColor
    {
        set => StatusProgressBar.Foreground = value;
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private string _status = string.Empty;

    public bool OpenButtonEnabled
    {
        get => _openButtonEnabled;
        set => SetField(ref _openButtonEnabled, value);
    }

    private bool _openButtonEnabled;

    public Visibility OpenButtonVisibility { get; private set; }

    public Visibility CloseButtonVisibility { get; private set; }

    public ICommand ButtonPressedCommand { get; private set; }

    private string? _openShortcutPath;

    public StatusWindow(Scoop.Package package, Scoop.PackageAction action, string? openShortcutPath)
    {
        InitializeComponent();
        DataContext = this;

        WindowBackdropType = OSVersionHelper.IsWindows11() ? Wpf.Ui.Controls.WindowBackdropType.Acrylic : Wpf.Ui.Controls.WindowBackdropType.None;
        SystemThemeWatcher.Watch(this, WindowBackdropType);

        Package = package;
        IconUri = package.GetFaviconUri();

        var actionStr = action switch
        {
            Scoop.PackageAction.Install => Properties.Resources.package_action_install,
            Scoop.PackageAction.Uninstall => Properties.Resources.package_action_uninstall,
            Scoop.PackageAction.Update => Properties.Resources.package_action_update,
            _ => string.Empty,
        };
        ActionPrefix = $"{actionStr}: ";
        VersionSuffix = $" ({package.Version})";

        _openShortcutPath = openShortcutPath;

        // Check if openShortcutPath is valid
        var forceCloseButton = openShortcutPath == null;

        SetupButtons(action, forceCloseButton);

        ButtonPressedCommand = new ActionCommand(OnButtonPressed);
    }

    private void SetupButtons(Scoop.PackageAction action, bool forceCloseButton)
    {
        // Show "Close" button when no openShortcutPath is available
        if (forceCloseButton)
        {
            ShowCloseButton();
            return;
        }

        switch (action)
        {
            case Scoop.PackageAction.Install:
            case Scoop.PackageAction.Update:
                ShowOpenButton();
                break;

            case Scoop.PackageAction.Uninstall:
            default:
                ShowCloseButton();
                break;
        }

        void ShowOpenButton()
        {
            OpenButtonVisibility = Visibility.Visible;
            CloseButtonVisibility = Visibility.Collapsed;
        }

        void ShowCloseButton()
        {
            OpenButtonVisibility = Visibility.Collapsed;
            CloseButtonVisibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Close window after 30 seconds
    /// </summary>
    public void AutoCloseIn30s()
    {
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(30);
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            this.Close();
        };
        timer.Start();
    }

    /// <summary>
    /// Execute <see cref="OnOpenButtonClick"/> or <see cref="OnCloseButtonClick"/>
    /// based on which button is visible
    /// </summary>
    private void OnButtonPressed()
    {
        if (OpenButtonVisibility == Visibility.Visible)
        {
            if (OpenButtonEnabled)
            {
                OnOpenButtonClick(null!, null!);
            }
        }
        else
        {
            OnCloseButtonClick(null!, null!);
        }
    }

    /// <summary>
    /// Open program from OpenProgramPath
    /// </summary>
    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _openShortcutPath,
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            this.Close();
        }
    }

    /// <summary>
    /// Close window
    /// </summary>
    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
