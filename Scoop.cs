// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Wox.Infrastructure;

namespace Community.PowerToys.Run.Plugin.Scoop;

#pragma warning disable SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly

public partial class Scoop : IDisposable
{
    private sealed class ScoopSearchApiPayload
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

    public class Package
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Version { get; set; }

        public string Homepage { get; set; }
    }

    private const string ApiSearchUrl = "https://scoopsearch.search.windows.net/indexes/apps/docs/search?api-version=2020-06-30";

    private const string WebsiteEnvUrl = "https://raw.githubusercontent.com/ScoopInstaller/scoopinstaller.github.io/main/.env";

    /// <summary>
    /// Extract app name and ignore if it is a failed install
    /// </summary>
    [GeneratedRegex(@"^\s*(\S+)(?=\s+\S+)(?!.*Install failed)")]
    private static partial Regex InstalledAppsFilter();

    /// <summary>
    /// Extract bucket names
    /// </summary>
    [GeneratedRegex(@"^\s*(\S+)(?=\s+\S+)")]
    private static partial Regex InstalledBucketsFilter();

    /// <summary>
    /// Extract API key from scoop website env
    /// </summary>
    [GeneratedRegex(@"VITE_APP_AZURESEARCH_KEY\s=\s""([^""]+)""")]
    private static partial Regex ApiKeyFilter();

    /// <summary>
    /// Gets a list of all installed packages
    /// </summary>
    public HashSet<string> InstalledPackages => _installedPackages ??= GetInstalledPackages();

    private HashSet<string> _installedPackages;

    /// <summary>
    /// Gets a list of all installed buckets
    /// </summary>
    public HashSet<string> InstalledBuckets => _installedBuckets ??= GetInstalledBuckets();

    private HashSet<string> _installedBuckets;

    /// <summary>
    /// Gets a value indicating whether there was an error while trying to get the API key or not
    /// </summary>
    public bool HasApiKeyError { get; private set; }

    /// <summary>
    /// Gets the text describing the error
    /// </summary>
    public string ApiKeyErrorText { get; private set; }

    private HttpClient _httpClient = new();
    private CancellationTokenSource _searchTokenSource = new();

    public Scoop()
    {
        GetApiKey();
        GetInstalledPackages();
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
                HasApiKeyError = true;
                ApiKeyErrorText = "Regex didn't match.";
            }
        }
        catch (Exception e)
        {
            HasApiKeyError = true;
            ApiKeyErrorText = e.Message;
        }
    }

    public async Task<List<Package>> SearchPackagesAsync(string query)
    {
        if (HasApiKeyError)
        {
            return [];
        }

        try
        {
            // Cancel previous search request
            _searchTokenSource.Cancel();
            _searchTokenSource.Dispose();
            _searchTokenSource = new CancellationTokenSource();

            var payload = new ScoopSearchApiPayload
            {
                Filter = "Metadata/OfficialRepositoryNumber eq 1 and Metadata/DuplicateOf eq null",
                OrderBy = "search.score() desc, Metadata/OfficialRepositoryNumber desc, NameSortable asc",
                Search = query,
                SearchMode = "all",
                Select = "Id,Name,Description,Homepage,Version",
                Top = 5,
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

    private HashSet<string> GetInstalledPackages()
    {
        var output = RunScoopCmdWithOutput("list");

        var matcher = InstalledAppsFilter();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var packages = new HashSet<string>();

        // Skip to 4th line where the app list begins and extract app names
        for (int i = 3; i < lines.Length; i++)
        {
            var match = matcher.Match(lines[i]);
            if (match.Success)
            {
                var packageName = match.Groups[1].Value;
                packages.Add(packageName);
            }
        }

        return packages;
    }

    private HashSet<string> GetInstalledBuckets()
    {
        var output = RunScoopCmdWithOutput("bucket list");

        var matcher = InstalledBucketsFilter();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var buckets = new HashSet<string>();

        // Skip to 3rd line where the app list begins and extract app names
        for (int i = 2; i < lines.Length; i++)
        {
            var match = matcher.Match(lines[i]);
            if (match.Success)
            {
                buckets.Add(match.Groups[1].Value);
            }
        }

        return buckets;
    }

    public bool Install(Package package)
    {
        var result = RunScoopCmd($"install {package.Name}");

        if (result)
        {
            _installedPackages.Add(package.Name);
        }

        return result;
    }

    public bool Uninstall(Package package)
    {
        var result = RunScoopCmd($"uninstall {package.Name}");

        if (result)
        {
            _installedPackages.Remove(package.Name);
        }

        return result;
    }

    public bool Update(Package package)
    {
        return RunScoopCmd($"update {package.Name}");
    }

    private bool RunScoopCmd(string parameters)
    {
        return Helper.OpenInShell("scoop", $"{parameters} && pause");
    }

    private string RunScoopCmdWithOutput(string parameters)
    {
        using var process = new Process();
        process.StartInfo.FileName = "cmd";
        process.StartInfo.Arguments = $"/c scoop {parameters}";
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
