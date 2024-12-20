using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors.Core;
using Common.UI;
using ManagedCommon;

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

    private readonly string? _openShortcutPath;

    // The enum flag for DwmSetWindowAttribute's second parameter, which tells the function what attribute to set.
    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
    }

    // The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function
    // what value of the enum to set.
    // Copied from dwmapi.h
    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3,
    }

    // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    internal static extern void DwmSetWindowAttribute(
        IntPtr hwnd,
        DWMWINDOWATTRIBUTE attribute,
        ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
        uint cbAttribute);

    public StatusWindow(Scoop.Package package, Scoop.PackageAction action, string? openShortcutPath)
    {
        SetSystemTheme();
        
        InitializeComponent();
        DataContext = this;

        UpdateWindowBackground();
        UpdateTitleBarButtonsVisibility();

        WindowChrome.SetWindowChrome(
            this,
            new WindowChrome
            {
                CaptionHeight = 50,
                CornerRadius = default,
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = ResizeMode == ResizeMode.NoResize ? default : new Thickness(4),
                UseAeroCaptionButtons = true
            }
        );

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

    private void OnSourceInitialized(object sender, EventArgs e)
    {
        if (OSVersionHelper.IsWindows11())
        {
            // ResizeMode="NoResize" removes rounded corners. So force them to rounded.
            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)!).EnsureHandle();
            DWMWINDOWATTRIBUTE attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            DWM_WINDOW_CORNER_PREFERENCE preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));
        }
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

    private void UpdateWindowBackground()
    {
        if((!Utility.IsBackdropDisabled() && 
            !Utility.IsBackdropSupported()))
        {
            this.SetResourceReference(BackgroundProperty, "WindowBackground");
        }
    }

    private void UpdateTitleBarButtonsVisibility()
    {
        if (Utility.IsBackdropDisabled() || !Utility.IsBackdropSupported() || SystemParameters.HighContrast)
            TitleBarCloseButton.Visibility = Visibility.Visible;
        else
            TitleBarCloseButton.Visibility = Visibility.Collapsed;
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
    
    // Based on From PowerLauncher.Helper.ThemeManager.SetSystemTheme()
    private void SetSystemTheme()
    {
        this.Resources.MergedDictionaries.Clear();
        
        var theme = new ThemeManager(Application.Current).GetCurrentTheme();
        
        // WPF Fluent theme
        const string fluentThemePathPrefix = "pack://application:,,,/PresentationFramework.Fluent;component/Themes";
        AddResourceDictionary(theme == Theme.Light 
            ? $"{fluentThemePathPrefix}/Fluent.Light.xaml"
            : $"{fluentThemePathPrefix}/Fluent.Dark.xaml");
        
        // Custom Fluent styles
        const string customThemePathPrefix = "pack://application:,,,/Community.PowerToys.Run.Plugin.Scoop;component/Themes";
        AddResourceDictionary(theme == Theme.Light 
            ? $"{customThemePathPrefix}/Fluent.Light.xaml"
            : $"{customThemePathPrefix}/Fluent.Dark.xaml");
        AddResourceDictionary($"{customThemePathPrefix}/Fluent.xaml");
        
        if (!OSVersionHelper.IsWindows11())
        {
            // Apply background only on Windows 10
            // Windows theme does not work properly for dark and light mode so right now set the background color manual.
            this.Background = new SolidColorBrush
            {
                Color = theme is Theme.Dark ? (Color)ColorConverter.ConvertFromString("#202020") : (Color)ColorConverter.ConvertFromString("#fafafa"),
            };
        }
        return;
        
        void AddResourceDictionary(string absolutePath) => this.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(absolutePath, UriKind.Absolute),
        });
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
