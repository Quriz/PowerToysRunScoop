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
using Microsoft.Extensions.Configuration;
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

    private const string SearchUrl = "https://scoopsearch.search.windows.net/indexes/apps/docs/search?api-version=2020-06-30";

    public HashSet<string> InstalledApps => _installedApps ??= GetInstalledApps();

    private HashSet<string> _installedApps;

    private HttpClient _httpClient = new();
    private CancellationTokenSource _searchTokenSource = new();

    // Extract app name and ignore if it is a failed install
    [GeneratedRegex("^\\s*(\\S+)(?=\\s+\\S+)(?!.*Install failed)")]
    private static partial Regex InstalledAppsFilter();

    public Scoop()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Scoop>()
            .Build();

        _httpClient.DefaultRequestHeaders.Add("api-key", config["ApiKey"]);

        GetInstalledApps();
    }

    public async Task<List<Package>> SearchPackagesAsync(string query)
    {
        try
        {
            // Cancel previous search request
            _searchTokenSource.Cancel();
            _searchTokenSource.Dispose();
            _searchTokenSource = new();

            var payload = new ScoopSearchApiPayload
            {
                Filter = "Metadata/OfficialRepositoryNumber eq 1 and Metadata/DuplicateOf eq null",
                OrderBy = "search.score() desc, Metadata/OfficialRepositoryNumber desc, NameSortable asc",
                Search = query,
                SearchMode = "all",
                Select = "Id,Name,Description,Homepage,Version",
                Top = 5,
            };

            var response = await _httpClient.PostAsJsonAsync(SearchUrl, payload, _searchTokenSource.Token);
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

    private HashSet<string> GetInstalledApps()
    {
        using var process = new Process();
        process.StartInfo.FileName = "cmd";
        process.StartInfo.Arguments = "/c scoop list";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var matcher = InstalledAppsFilter();
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var apps = new HashSet<string>();

        // Skip to 4th line where the app list begins and extract app names
        for (int i = 3; i < lines.Length; i++)
        {
            var match = matcher.Match(lines[i]);
            if (match.Success)
            {
                apps.Add(match.Groups[1].Value);
            }
        }

        return apps;
    }

    public bool Install(Package package)
    {
        var result = RunScoopCmd($"install {package.Name}");

        if (result)
        {
            _installedApps.Add(package.Name);
        }

        return result;
    }

    public bool Uninstall(Package package)
    {
        var result = RunScoopCmd($"uninstall {package.Name}");

        if (result)
        {
            _installedApps.Remove(package.Name);
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

    public void Dispose()
    {
        _httpClient?.Dispose();
        _searchTokenSource?.Dispose();

        GC.SuppressFinalize(this);
    }
}

#pragma warning restore SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly
