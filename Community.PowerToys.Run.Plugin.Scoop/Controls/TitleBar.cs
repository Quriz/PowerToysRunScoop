using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Community.PowerToys.Run.Plugin.Scoop.Controls;

public partial class TitleBar
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(TitleBar));

    public ImageSource Icon
    {
        get { return (ImageSource)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar));

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }
    
    private Window? _parentWindow;
    
    public TitleBar()
    {
        InitializeComponent();
        DataContext = this;
        
        UpdateTitleBarButtonsVisibility();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var parentWindow = VisualTreeHelper.GetParent(this);

        while (parentWindow is not null and not Window)
            parentWindow = VisualTreeHelper.GetParent(parentWindow);

        _parentWindow = parentWindow as Window;
    }

    private void UpdateTitleBarButtonsVisibility()
    {
        if (Utility.IsBackdropDisabled() || !Utility.IsBackdropSupported() || SystemParameters.HighContrast)
            TitleBarCloseButton.Visibility = Visibility.Visible;
        else
            TitleBarCloseButton.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Close window
    /// </summary>
    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        _parentWindow?.Close();
    }
}
