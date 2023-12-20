// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wox.Infrastructure;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Scoop;

#pragma warning disable SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly

public partial class Scoop : IDisposable
{
    private sealed class ApiSearchPayload
    {
        [JsonPropertyName("filter")]
        public string Filter { get; set; }

        [JsonPropertyName("orderby")]
        public string OrderBy { get; set; }

        [JsonPropertyName("search")]
        public string Search { get; set; }

        [JsonPropertyName("searchMode")]
        public string SearchMode { get; set; }

        [JsonPropertyName("select")]
        public string Select { get; set; }

        [JsonPropertyName("top")]
        public int Top { get; set; }
    }

    private sealed class SearchApiResponse
    {
        [JsonPropertyName("value")]
        public List<Package> Packages { get; set; }
    }

    public record PackageMetadata(string Repository);

    public record Package(string Name, string Description, string Version, string Homepage, PackageMetadata Metadata);

    private const string ApiSearchUrl = "https://scoopsearch.search.windows.net/indexes/apps/docs/search?api-version=2020-06-30";

    private const string OfficialBucketsUrl = "https://raw.githubusercontent.com/ScoopInstaller/Scoop/master/buckets.json";

    private const string WebsiteEnvUrl = "https://raw.githubusercontent.com/ScoopInstaller/scoopinstaller.github.io/main/.env";

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
    /// Gets a list of all official buckets in name/url pairs
    /// </summary>
    public Dictionary<string, string> OfficialBuckets { get; private set; }

    /// <summary>
    /// Gets a list of all source URLs of installed buckets
    /// </summary>
    public HashSet<string> InstalledBucketSourceUrls { get; private set; }

    /// <summary>
    /// Gets a list of all installed packages
    /// </summary>
    public HashSet<string> InstalledPackages { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance is initialized or not
    /// </summary>
    public bool IsInitialized { get; private set; }

    private HttpClient _httpClient = new();
    private CancellationTokenSource _searchTokenSource = new();

    public Scoop()
    {
        try
        {
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
            OfficialBuckets = _httpClient.GetFromJsonAsync<Dictionary<string, string>>(OfficialBucketsUrl).Result;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to get list of official buckets.", e);
        }
    }

    private void GetInstalledBuckets()
    {
        var output = RunCmdWithOutput("scoop bucket list");

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
        var output = RunCmdWithOutput("scoop list");

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
            // Cancel previous search request
            _searchTokenSource.Cancel();
            _searchTokenSource.Dispose();
            _searchTokenSource = new CancellationTokenSource();

            var payload = new ApiSearchPayload
            {
                Filter = "Metadata/OfficialRepositoryNumber eq 1 and Metadata/DuplicateOf eq null",
                OrderBy = "search.score() desc, Metadata/OfficialRepositoryNumber desc, NameSortable asc",
                Search = query,
                SearchMode = "all",
                Select = "Name,Description,Version,Homepage,Metadata/Repository",
                Top = 6,
            };

            var response = await _httpClient.PostAsJsonAsync(ApiSearchUrl, payload, _searchTokenSource.Token);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<SearchApiResponse>(_searchTokenSource.Token);
                return apiResponse.Packages;
            }
        }
        catch (Exception)
        {
            // do nothing
        }

        return [];
    }

    public bool Install(Package package)
    {
        // Get bucket name from matching source URL
        var bucketName = OfficialBuckets.FirstOrDefault(x => x.Value == package.Metadata.Repository).Key;

        var addBucketCommand = $"scoop bucket add {bucketName}";
        var installPackageCommand = $"scoop install {bucketName}/{package.Name}";
        var result = false;

        // Check if bucket for this package is already installed or not
        if (InstalledBucketSourceUrls.Contains(package.Metadata.Repository))
        {
            result = OpenCmdWindow(installPackageCommand);
        }
        else
        {
            // Ask to add bucket
            var message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.message_add_bucket, bucketName);
            var choice = MessageBox.Show(message, "Scoop", MessageBoxButton.YesNo);
            if (choice != MessageBoxResult.Yes)
            {
                return false;
            }

            result = OpenCmdWindow($"{addBucketCommand} && {installPackageCommand}");
        }

        if (result)
        {
            InstalledPackages.Add(package.Name);
        }

        return result;
    }

    public bool Uninstall(Package package)
    {
        var result = OpenCmdWindow($"scoop uninstall {package.Name}");

        if (result)
        {
            InstalledPackages.Remove(package.Name);
        }

        return result;
    }

    public bool Update(Package package)
    {
        return OpenCmdWindow($"scoop update {package.Name}");
    }

    private static bool OpenCmdWindow(string command)
    {
        return Helper.OpenInShell("cmd", $"/c {command} && pause");
    }

    private static string RunCmdWithOutput(string command)
    {
        using var process = new Process();
        process.StartInfo.FileName = "cmd";
        process.StartInfo.Arguments = $"/c {command}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _searchTokenSource?.Dispose();

        GC.SuppressFinalize(this);
    }
}

#pragma warning restore SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly
