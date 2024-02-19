// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Wox.Plugin.Logger;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace Community.PowerToys.Run.Plugin.Scoop;

#pragma warning disable SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly

public partial class Scoop : IDisposable
{
    private sealed class ApiSearchPayload
    {
        [JsonPropertyName("filter")]
        public required string Filter { get; set; }

        [JsonPropertyName("orderby")]
        public required string OrderBy { get; set; }

        [JsonPropertyName("search")]
        public required string Search { get; set; }

        [JsonPropertyName("searchMode")]
        public required string SearchMode { get; set; }

        [JsonPropertyName("select")]
        public required string Select { get; set; }

        [JsonPropertyName("top")]
        public int Top { get; set; }
    }

    private sealed class SearchApiResponse
    {
        [JsonPropertyName("value")]
        public required List<Package> Packages { get; set; }
    }

    public record PackageMetadata(string Repository, string FilePath);

    public record Package(string Name, string Description, string Version, string Homepage, PackageMetadata Metadata);

    public enum PackageAction
    {
        Install,
        Uninstall,
        Update,
    }

    private const string ApiSearchUrl = "https://scoopsearch.search.windows.net/indexes/apps/docs/search?api-version=2020-06-30";

    private const string OfficialBucketsUrl = "https://raw.githubusercontent.com/ScoopInstaller/Scoop/master/buckets.json";

    private const string WebsiteEnvUrl = "https://raw.githubusercontent.com/ScoopInstaller/scoopinstaller.github.io/main/.env";

    private static readonly CompositeFormat MessageAddBucketFormat = CompositeFormat.Parse(Properties.Resources.message_add_bucket);

    /// <summary>
    /// Extract package name and ignore if it is a failed install
    /// </summary>
    [GeneratedRegex(@"^\s*(\S+)(?=\s+\S+)(?!.*Install failed)")]
    private static partial Regex InstalledPackagesFilter();

    /// <summary>
    /// Extract source URLs of installed bucket
    /// </summary>
    [GeneratedRegex(@"https://\S+")]
    private static partial Regex InstalledBucketsFilter();

    /// <summary>
    /// Extract API key from scoop website env
    /// </summary>
    [GeneratedRegex(@"VITE_APP_AZURESEARCH_KEY\s=\s""([^""]+)""")]
    private static partial Regex ApiKeyFilter();

    /// <summary>
    /// Gets or sets a list of all official buckets in name/url pairs
    /// </summary>
    private Dictionary<string, string> OfficialBuckets { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of all source URLs of installed buckets
    /// </summary>
    private HashSet<string> InstalledBucketSourceUrls { get; set; } = [];

    /// <summary>
    /// Gets a list of all installed packages
    /// </summary>
    public HashSet<string> InstalledPackages { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether this instance is initialized or not
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether scoop is installed or not
    /// </summary>
    public bool IsScoopInstalled { get; private set; }

    private readonly HttpClient _httpClient = new();
    private CancellationTokenSource _searchTokenSource = new();

    public void Init()
    {
        try
        {
            CheckIfScoopInstalled();
            if (!IsScoopInstalled)
            {
                return;
            }

            GetApiKey();
            GetOfficialBuckets();

            GetInstalledBuckets();
            GetInstalledPackages();

            IsInitialized = true;
        }
        catch (Exception e)
        {
            Log.Error($"Initialization failed: {e.Message}", GetType());
            throw;
        }
    }

    /// <summary>
    /// Checks if scoop is installed
    /// </summary>
    private void CheckIfScoopInstalled()
    {
        using var process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c scoop -v";
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        process.WaitForExit();

        IsScoopInstalled = process.ExitCode == 0;
    }

    /// <summary>
    /// Extract the API key from the .env file of scoop.sh website source code
    /// </summary>
    private void GetApiKey()
    {
        try
        {
            var env = _httpClient.GetStringAsync(WebsiteEnvUrl).Result;

            var filter = ApiKeyFilter();
            var match = filter.Match(env);

            if (match.Success)
            {
                var apiKey = match.Groups[1].Value;
                _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
            }
            else
            {
                throw new InvalidOperationException("Regex for API key didn't match.");
            }
        }
        catch (Exception e)
        {
            // Propagate the exception
            throw new InvalidOperationException("Failed to get API key.", e);
        }
    }

    private void GetOfficialBuckets()
    {
        try
        {
            OfficialBuckets = _httpClient.GetFromJsonAsync<Dictionary<string, string>>(OfficialBucketsUrl).Result!;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to get list of official buckets.", e);
        }
    }

    private void GetInstalledBuckets()
    {
        if (!Helper.RunCmdWithOutput("scoop bucket list", out var output))
        {
            InstalledBucketSourceUrls = [];
            return;
        }

        var matcher = InstalledBucketsFilter();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var buckets = new HashSet<string>();

        // Skip to 3rd line where the app list begins and extract app names
        for (int i = 2; i < lines.Length; i++)
        {
            var match = matcher.Match(lines[i]);
            if (match.Success)
            {
                buckets.Add(match.Value);
            }
        }

        InstalledBucketSourceUrls = buckets;
    }

    private void GetInstalledPackages()
    {
        if (!Helper.RunCmdWithOutput("scoop list", out var output))
        {
            InstalledPackages = [];
            return;
        }

        var matcher = InstalledPackagesFilter();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var packages = new HashSet<string>();

        // Skip to 4th line where the app list begins and extract app names
        for (int i = 3; i < lines.Length; i++)
        {
            var match = matcher.Match(lines[i]);
            if (match.Success)
            {
                packages.Add(match.Groups[1].Value);
            }
        }

        InstalledPackages = packages;
    }

    public async Task<List<Package>> SearchPackagesAsync(string query)
    {
        if (!IsInitialized)
        {
            return [];
        }

        try
        {
            // Cancel previous search request and create new token for this request
            _searchTokenSource.Cancel();
            _searchTokenSource.Dispose();
            _searchTokenSource = new CancellationTokenSource();

            // Search for query via API
            var payload = new ApiSearchPayload
            {
                Filter = "Metadata/OfficialRepositoryNumber eq 1 and Metadata/DuplicateOf eq null",
                OrderBy = "search.score() desc, Metadata/OfficialRepositoryNumber desc, NameSortable asc",
                Search = query,
                SearchMode = "all",
                Select = "Name,Description,Version,Homepage,Metadata/Repository,Metadata/FilePath",
                Top = 6,
            };

            var response = await _httpClient.PostAsJsonAsync(ApiSearchUrl, payload, _searchTokenSource.Token);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<SearchApiResponse>(_searchTokenSource.Token);
                return apiResponse!.Packages;
            }
        }
        catch
        {
            // do nothing
        }

        return [];
    }

    /// <summary>
    /// Install the given package
    /// </summary>
    /// <param name="package">The package to install</param>
    public void Install(Package package)
    {
        // Get bucket name from matching source URL
        var bucketName = OfficialBuckets.FirstOrDefault(x => x.Value == package.Metadata.Repository).Key;

        var addBucketCommand = $"scoop bucket add {bucketName}";
        var installPackageCommand = $"scoop install {bucketName}/{package.Name}";

        // Check if bucket for this package is already installed or not
        if (InstalledBucketSourceUrls.Contains(package.Metadata.Repository))
        {
            RunCmdInStatusWindow(installPackageCommand, package, PackageAction.Install, OnExit);
        }
        else
        {
            // Ask to add bucket
            var choice = Helper.ShowMessageBoxYesNo("PowerToys Run: Scoop", string.Format(CultureInfo.CurrentCulture, MessageAddBucketFormat, bucketName));
            if (choice != MessageBoxResult.Primary)
            {
                return;
            }

            RunCmdInStatusWindow($"{addBucketCommand} && {installPackageCommand}", package, PackageAction.Install, OnExit);
        }

        void OnExit(bool success)
        {
            if (success)
            {
                InstalledPackages.Add(package.Name);
            }
        }
    }

    /// <summary>
    /// Uninstall the given package
    /// </summary>
    /// <param name="package">The package to uninstall</param>
    public void Uninstall(Package package)
    {
        RunCmdInStatusWindow($"scoop uninstall {package.Name}", package, PackageAction.Uninstall, OnExit);

        void OnExit(bool success)
        {
            if (success)
            {
                InstalledPackages.Remove(package.Name);
            }
        }
    }

    /// <summary>
    /// Update the given package
    /// </summary>
    /// <param name="package">The package to update</param>
    public void Update(Package package)
    {
        RunCmdInStatusWindow($"scoop update {package.Name}", package, PackageAction.Update, null);
    }

    /// <summary>
    /// Run a install/uninstall/update command and show a window updating it's progress
    /// </summary>
    /// <param name="command">The command to execute in background</param>
    /// <param name="package">The package that gets installed/uninstalled/updated</param>
    /// <param name="action">Specifies which action the command executes (install/uninstall/update)</param>
    /// <param name="onExit">Event that gets called at the end with a boolean if the action was successful</param>
    private void RunCmdInStatusWindow(string command, Package package, PackageAction action, Action<bool>? onExit)
        => Application.Current.Dispatcher.Invoke(async () =>
        {
            // Get shortcut path to be able to open program after install or update
            // Ignore for uninstall action
            var shortcutPath = action switch
            {
                PackageAction.Install or PackageAction.Update => await GetShortcutPath(package),
                _ => null,
            };

            // Create window
            var window = new StatusWindow(package, action, shortcutPath);
            window.Show();
            window.Activate();

            var fullLog = string.Empty;
            var currentLine = string.Empty;
            var currentProgress = -1;

            // Run command
            await Helper.RunCmdWithOutputCallback(command, OnCharRead);

            // Hide progress bar if command exited before starting action
            if (currentProgress == -1)
            {
                window.ProgressBarVisible = false;
            }

            window.Progress = 100;

            var finishedSuccessfully = currentProgress == 100;
            var hasError = !finishedSuccessfully && fullLog.Contains("ERROR");

            // Enable Button on success & warn but not on error
            if (!hasError && shortcutPath != null)
            {
                window.OpenButtonEnabled = true;
            }

            // Close the window in 30s if action was successful
            if (finishedSuccessfully)
            {
                window.AutoCloseIn30s();

                window.ProgressBarColor = Brushes.Green;
            }
            else
            {
                // Show the full log if there was a warning or error
                window.Status = fullLog.TrimEnd();
                window.StatusText.TextWrapping = TextWrapping.Wrap;

                if (hasError)
                {
                    window.ProgressBarColor = Brushes.Red;
                }
            }

            onExit?.Invoke(finishedSuccessfully);
            return;

            // Process output live for every character
            void OnCharRead(char character)
            {
                currentLine += character;

                // Detect line end
                if (character == '\n')
                {
                    var line = currentLine.TrimEnd();
                    currentLine = string.Empty;
                    
                    // Cancel if line is a bucket update entry 
                    if (line.StartsWith(" *", StringComparison.Ordinal))
                    {
                        return;
                    }
                    
                    OnLineRead(line);
                }

                // Display current line if not empty
                if (!string.IsNullOrWhiteSpace(currentLine))
                {
                    window.Status = currentLine.TrimEnd();
                }
            }

            // Process output line by line
            void OnLineRead(string line)
            {
                fullLog += line + '\n';
                
                // Approximate progress from line
                var progress = line switch
                {
                    not null when line.StartsWith("Installing", StringComparison.Ordinal) => 10,
                    not null when line.StartsWith("Uninstalling", StringComparison.Ordinal) => 10,
                    not null when line.StartsWith("Updating one", StringComparison.Ordinal) => 10,
                    not null when line.StartsWith("Downloading", StringComparison.Ordinal) => 20,
                    not null when line.StartsWith("Loading", StringComparison.Ordinal) => 20,
                    not null when line.StartsWith("Checking hash", StringComparison.Ordinal) => 40,
                    not null when line.StartsWith("Extracting", StringComparison.Ordinal) => 50,
                    not null when line.StartsWith("Linking", StringComparison.Ordinal) => 90,
                    not null when line.EndsWith("installed successfully!", StringComparison.Ordinal) => 100,
                    not null when line.EndsWith("was uninstalled.", StringComparison.Ordinal) => 100,
                    not null when line.StartsWith("Latest versions for all apps are installed!", StringComparison.Ordinal) => 100,
                    _ => currentProgress,
                };

                // Let the progress only go higher
                currentProgress = Math.Max(currentProgress, progress);

                window.Progress = currentProgress;
            }
        });

    /// <summary>
    /// Extracts first shortcut name from the package manifest (via web request) and builds the according shortcut path
    /// </summary>
    /// <param name="package">The package to get the shortcut for</param>
    /// <returns>The path of the shortcut file</returns>
    private async Task<string?> GetShortcutPath(Package package)
    {
        try
        {
            // Get manifest
            var repositoryUrl = package.Metadata.Repository.Replace("github.com", "raw.githubusercontent.com");
            var manifestUrl = $"{repositoryUrl}/master/{package.Metadata.FilePath}";
            var jsonString = await _httpClient.GetStringAsync(manifestUrl);

            var jsonDocument = JsonDocument.Parse(jsonString);
            var root = jsonDocument.RootElement;

            // Get shortcuts
            if (root.TryGetProperty("shortcuts", out var shortcutsElement))
            {
                foreach (var shortcutElement in shortcutsElement.EnumerateArray().Where(shortcutElement => shortcutElement.GetArrayLength() == 2))
                {
                    // Get shortcut name and build file path
                    var shortcutName = shortcutElement[1].GetString();
                    var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                    return Path.Combine(startMenu, "Programs", "Scoop Apps", $"{shortcutName}.lnk");
                }
            }
        }
        catch
        {
            // do nothing
        }

        return null;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _searchTokenSource?.Dispose();

        GC.SuppressFinalize(this);
    }
}

#pragma warning restore SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly
