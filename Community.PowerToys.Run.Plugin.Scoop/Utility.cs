using System.Diagnostics;
using System.Windows;

namespace Community.PowerToys.Run.Plugin.Scoop;

public static class Utility
{
    /// <summary>
    /// Get favicon URI for homepage of package
    /// </summary>
    /// <param name="package">Package to get homepage from</param>
    /// <returns>Favicon URI</returns>
    public static Uri GetFaviconUri(this Scoop.Package package)
    {
        return new Uri(new Uri(package.Homepage), "/favicon.ico");
    }

    /// <summary>
    /// Opens a WPF UI MessageBox with Yes/No buttons
    /// </summary>
    /// <param name="message">The message to show</param>
    /// <returns>Which button was clicked</returns>
    public static MessageBoxResult ShowMessageBoxYesNo(string message)
        => Application.Current.Dispatcher.Invoke(() =>
        {
            return MessageBox.Show(message, "PowerToys Run: Scoop", MessageBoxButton.YesNo, MessageBoxImage.Information);
            // var uiMessageBox = new MessageBox
            // {
            //     Title = "PowerToys Run: Scoop",
            //     Content = message,
            //     PrimaryButtonText = Properties.Resources.messagebox_yes,
            //     CloseButtonText = Properties.Resources.messagebox_no,
            // };
            // return uiMessageBox.ShowDialogAsync().Result;
        });

    /// <summary>
    /// Run the command in background and read its output.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="output">The read output from the process</param>
    /// <returns>If the exit code was 0 (successful)</returns>
    public static bool RunCmdWithOutput(string command, out string output)
    {
        using var process = new Process();
        process.StartInfo.FileName = "cmd";
        process.StartInfo.Arguments = $"/c {command}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    /// <summary>
    /// Run the command in background and read its output.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="onCharRead">Callback to read output from the process for every character</param>
    public static async Task RunCmdWithOutputCallback(string command, Action<char> onCharRead)
    {
        using var process = new Process();
        process.StartInfo.FileName = "cmd";
        process.StartInfo.Arguments = $"/c {command}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        using var reader = process.StandardOutput;
        var buffer = new char[1];

        while (await reader.ReadAsync(buffer, 0, buffer.Length) > 0)
        {
            var character = buffer[0];
            onCharRead?.Invoke(character);
        }

        await process.WaitForExitAsync();
    }
    
    // Copied from https://github.com/microsoft/WPF-Samples/blob/main/Sample%20Applications/WPFGallery/Helpers/Utility.cs
    public static bool IsBackdropSupported()
    {
        var os = Environment.OSVersion;
        var version = os.Version;

        return version.Major >= 10 && version.Build >= 22621;
    }

    // Copied from https://github.com/microsoft/WPF-Samples/blob/main/Sample%20Applications/WPFGallery/Helpers/Utility.cs
    public static bool IsBackdropDisabled()
    {
        var appContextBackdropData = AppContext.GetData("Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop");
        bool disableFluentThemeWindowBackdrop = false;

        if (appContextBackdropData != null)
        {
            disableFluentThemeWindowBackdrop = bool.Parse(Convert.ToString(appContextBackdropData));
        }

        return disableFluentThemeWindowBackdrop;
    }
}