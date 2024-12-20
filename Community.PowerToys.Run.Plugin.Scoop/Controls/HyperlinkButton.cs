using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Community.PowerToys.Run.Plugin.Scoop.Controls;

public class HyperlinkButton : Button
{
    /// <summary>Identifies the <see cref="NavigateUri"/> dependency property.</summary>
    public static readonly DependencyProperty NavigateUriProperty = DependencyProperty.Register(
        nameof(NavigateUri),
        typeof(string),
        typeof(HyperlinkButton),
        new PropertyMetadata(string.Empty)
    );

    /// <summary>
    /// Gets or sets the URL (or application shortcut) to open.
    /// </summary>
    public string NavigateUri
    {
        get => GetValue(NavigateUriProperty) as string ?? string.Empty;
        set => SetValue(NavigateUriProperty, value);
    }

    protected override void OnClick()
    {
        base.OnClick();
        
        if (string.IsNullOrEmpty(NavigateUri))
            return;

        try
        {
            var startInfo = new ProcessStartInfo(new Uri(NavigateUri).AbsoluteUri) { UseShellExecute = true };
            _ = Process.Start(startInfo);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }
}