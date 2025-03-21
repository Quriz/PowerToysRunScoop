﻿using System.Globalization;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ManagedCommon;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Scoop;

#pragma warning disable SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly

public class Main : IPlugin, IPluginI18n, IDelayedExecutionPlugin, IContextMenu, IDisposable
{
    public string Name => Properties.Resources.plugin_name;

    public string Description => Properties.Resources.plugin_description;

    public static string PluginID => "be0142a36ee54bd6ab789086d5828b4b";

    private static readonly CompositeFormat PluginNoResultFormat = CompositeFormat.Parse(Properties.Resources.plugin_no_result);
    private static readonly CompositeFormat ErrorHomepageFormat = CompositeFormat.Parse(Properties.Resources.error_homepage);

    private Action<string> onPluginError = null!;

    private Scoop _scoop = null!;
    private bool _isInitializing = false;

    private PluginInitContext _context = null!;
    private static string _iconPath = null!;
    private bool _disposed;
    

    public void Init(PluginInitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _context.API.ThemeChanged += OnThemeChanged;
        UpdateIconPath(_context.API.GetCurrentTheme());

        DefaultBrowserInfo.UpdateIfTimePassed();

        onPluginError = message =>
        {
            Log.Error(message, GetType());
            _context.API.ShowMsg($"Plugin: {Properties.Resources.plugin_name}", message);
        };

        _scoop = new Scoop();
        Task.Run(InitScoop);
    }

    /// <summary>
    /// Try to initialize Scoop class and retry a few times on error
    /// </summary>
    private async void InitScoop()
    {
        if (_isInitializing)
            return;
        
        _isInitializing = true;
        
        for (int i = 0; i < 7; i++)
        {
            try
            {
                _scoop.Init();
                return;
            }
            catch (Exception)
            {
                // ignored
            }

            // Wait 30s before retrying
            await Task.Delay(30000);
        }
        
        _isInitializing = false;
    }

    public List<Result> Query(Query query) => Query(query, false);

    public List<Result> Query(Query query, bool delayedExecution)
    {
        if (!_scoop.IsScoopInstalled)
        {
            return ScoopNotInstalledQueryResult();
        }

        if (!_scoop.IsInitialized)
        {
            Task.Run(InitScoop);
            return IsInitializingQueryResult();
        }

        if (!delayedExecution || query is null || string.IsNullOrWhiteSpace(query.Search))
        {
            return DefaultQueryResult();
        }

        var searchTerm = query.Search.Trim();

        if (searchTerm.Length > 1)
        {
            var packages = _scoop.SearchPackagesAsync(searchTerm).Result;
            var results = packages.Select(package => GetResult(package, searchTerm)).ToList();
            if (results.Count == 0)
            {
                return NotFoundQueryResult(searchTerm);
            }

            return results;
        }

        return DefaultQueryResult();
    }

    private List<Result> ScoopNotInstalledQueryResult()
    {
        return
        [
            new Result
            {
                Title = Properties.Resources.error_scoop_not_installed,
                SubTitle = Properties.Resources.error_scoop_not_installed_sub,
                QueryTextDisplay = " ", // Empty string doesn't work
                IcoPath = _iconPath,
                Action = _ => OpenUrlInBrowser("https://scoop.sh/"),
            },
        ];
    }

    private List<Result> IsInitializingQueryResult()
    {
        return
        [
            new Result
            {
                Title = Properties.Resources.is_init,
                SubTitle = Properties.Resources.is_init_sub,
                QueryTextDisplay = " ", // Empty string doesn't work
                IcoPath = _iconPath,
            },
        ];
    }

    private List<Result> NotFoundQueryResult(string searchTerm)
    {
        var title = string.Format(CultureInfo.CurrentCulture, PluginNoResultFormat, searchTerm);
        return
        [
            new Result
            {
                Title = title,
                TitleHighlightData = StringMatcher.FuzzySearch(searchTerm, title).MatchData,
                SubTitle = Properties.Resources.plugin_no_result_sub,
                QueryTextDisplay = searchTerm,
                IcoPath = _iconPath,
            },
        ];
    }

    private List<Result> DefaultQueryResult()
    {
        return
        [
            new Result
            {
                Title = Properties.Resources.plugin_description,
                SubTitle = Properties.Resources.plugin_description_sub,
                QueryTextDisplay = " ", // Empty string doesn't work
                IcoPath = _iconPath,
            },
        ];
    }

    private Result GetResult(Scoop.Package package, string searchTerm)
    {
        var installed = _scoop.InstalledPackages.Contains(package.Name);
        var installedSuffix = installed ? "(installed)" : string.Empty;

        return new Result
        {
            ContextData = package,
            Title = $"{package.Name} {installedSuffix}",
            TitleHighlightData = StringMatcher.FuzzySearch(searchTerm, package.Name).MatchData,
            SubTitle = package.Description,
            QueryTextDisplay = package.Name,
            Icon = () => new BitmapImage(package.GetFaviconUri()),
            Action = _ =>
            {
                _scoop.Install(package);
                return true;
            },
        };
    }

    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult?.ContextData is not Scoop.Package package)
        {
            return [];
        }

        var contextResults = new List<ContextMenuResult>();

        var isPackageInstalled = _scoop.InstalledPackages.Contains(package.Name);

        // Open homepage option
        contextResults.Add(new ContextMenuResult
        {
            PluginName = Name,
            Title = Properties.Resources.context_menu_homepage,
            Glyph = "\xF6FA", // WebSearch
            FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
            AcceleratorModifiers = ModifierKeys.Control,
            AcceleratorKey = Key.H,
            Action = _ => OpenUrlInBrowser(package.Homepage),
        });

        if (isPackageInstalled)
        {
            // Update option
            contextResults.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = Properties.Resources.context_menu_update,
                Glyph = "\xE777", // Update
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorModifiers = ModifierKeys.Control,
                AcceleratorKey = Key.U,
                Action = _ =>
                {
                    _scoop.Update(package);
                    return true;
                },
            });

            // Uninstall option
            contextResults.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = Properties.Resources.context_menu_uninstall,
                Glyph = "\xE74D", // Delete
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorModifiers = ModifierKeys.Control,
                AcceleratorKey = Key.D,
                Action = _ =>
                {
                    _scoop.Uninstall(package);
                    return true;
                },
            });
        }
        else
        {
            // Install option
            contextResults.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = Properties.Resources.context_menu_install,
                Glyph = "\xE896", // Download
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                Action = _ =>
                {
                    _scoop.Install(package);
                    return true;
                },
            });
        }

        return contextResults;
    }

    private bool OpenUrlInBrowser(string url)
    {
        if (Helper.OpenInShell(DefaultBrowserInfo.Path, url))
        {
            return true;
        }

        onPluginError(string.Format(CultureInfo.CurrentCulture, ErrorHomepageFormat, DefaultBrowserInfo.Name ?? DefaultBrowserInfo.MSEdgeName));
        return false;
    }

    public string GetTranslatedPluginTitle() => Properties.Resources.plugin_name;

    public string GetTranslatedPluginDescription() => Properties.Resources.plugin_description;

    private void OnThemeChanged(Theme currentTheme, Theme newTheme)
    {
        UpdateIconPath(newTheme);
    }

    private static void UpdateIconPath(Theme theme)
    {
        _iconPath = "Images/scoop.png";
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_context != null && _context.API != null)
            {
                _context.API.ThemeChanged -= OnThemeChanged;
            }

            _scoop.Dispose();

            _disposed = true;
        }
    }
}

#pragma warning restore SA1010 // OpeningSquareBracketsMustBeSpacedCorrectly
