// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace Community.PowerToys.Run.Plugin.Scoop;

public static class Helper
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
    /// Open a WPF UI MessageBox with Yes/No
    /// </summary>
    /// <param name="title">The title of the MessageBox window</param>
    /// <param name="message">The message to show</param>
    /// <returns>Which button was clicked</returns>
    public static Wpf.Ui.Controls.MessageBoxResult ShowMessageBoxYesNo(string title, string message)
        => Application.Current.Dispatcher.Invoke(() =>
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
                PrimaryButtonText = Properties.Resources.messagebox_yes,
                CloseButtonText = Properties.Resources.messagebox_no,
            };
            return uiMessageBox.ShowDialogAsync().Result;
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
}
