// Copyright (c) Corey Hayward. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Community.PowerToys.Run.Plugin.Snippets.Models;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Snippets;

public class Main : IPlugin, ISettingProvider, IContextMenu
{
    private PluginInitContext _context;
    private string _iconPath;
    private int _beginTypeDelay;
    private string _pasterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Paster", "Paster.exe");
    private SnippetManager _snippetManager = new();

    public string Name => "Snippets";

    public string Description => "Snippet manager for searching and inserting saved snippets.";

    public static string PluginID => "fdcdb1688fe64916b8d0cbbeb8aadcb8";

    public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
    {
        new PluginAdditionalOption()
        {
            Key = "PasteDelay",
            DisplayLabel = "Paste Delay (ms)",
            DisplayDescription = "Sets how long in milliseconds to wait before paste occurs.",
            NumberValue = 200,
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
        },
    };

    public void Init(PluginInitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _context.API.ThemeChanged += OnThemeChanged;
        UpdateIconPath(_context.API.GetCurrentTheme());
    }

    public List<Result> Query(Query query)
    {
        if (string.IsNullOrWhiteSpace(query?.Search))
        {
            return _snippetManager.Snippets.Take(5).Select(CreateExistingSnippetResult).ToList();
        }

        var results = new List<Result>();
        var matchingSnippets = _snippetManager.Snippets
            .Where(x => x.Content.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase));

        if (!matchingSnippets.Any(x => x.Content == query.Search))
        {
            results.Add(CreateNewSnippetResult(query));
        }

        results.AddRange(matchingSnippets.Select(CreateExistingSnippetResult));
        return results;
    }

    private Result CreateExistingSnippetResult(Snippet item)
        => new Result()
        {
            Title = item.Title,
            SubTitle = item.Content,
            IcoPath = _iconPath,
            Action = (context) =>
            {
                Clipboard.SetText(item.Content);
                Task.Run(() => RunAsSTAThread(() =>
                {
                    Thread.Sleep(_beginTypeDelay);
                    SendKeys.SendWait("^(v)");
                }));
                return true;
            },
            ContextData = item,
        };

    private Result CreateNewSnippetResult(Query query)
    {
        var input = query.Search;
        Snippet snippet;
        var split = input.Split('-', 2);
        if (split.Length == 2)
        {
            snippet = new Snippet(split[0], split[1].Trim());
        }
        else
        {
            snippet = new Snippet(string.Empty, input.Trim());
        }

        return new Result()
        {
            Title = "Create a new snippet",
            SubTitle = $"{(string.IsNullOrWhiteSpace(snippet.Title) ? "[Empty Title]" : snippet.Title)} | {snippet.Content}",
            IcoPath = _iconPath,
            Action = (context) =>
            {
                _snippetManager.AddSnippet(snippet);
                _context.API.ChangeQuery(query.ActionKeyword, true);
                return true;
            },
        };
    }

    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult.ContextData is not Snippet snippet)
        {
            return [];
        }

        return
        [
            new()
            {
                Title = "Paste as administrator (Ctrl+Shift+Enter)",
                Glyph = "\xE7EF",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorKey = Key.Enter,
                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                PluginName = Name,
                Action = _ =>
                {
                    Clipboard.SetText(snippet.Content);
                    Task.Run(() => RunAsSTAThread(() =>
                    {
                        Thread.Sleep(_beginTypeDelay);
                        var foregroundWindow = GetForegroundWindow();
                        Helper.OpenInShell(_pasterPath, runAs: Helper.ShellRunAsType.Administrator, runWithHiddenWindow: true);
                        SetForegroundWindow(foregroundWindow);
                    }));

                    return true;
                },
            },
            new()
            {
                Title = "Delete Snippet (Shift+Enter)",
                Glyph = "\xE74D",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorKey = Key.Enter,
                AcceleratorModifiers = ModifierKeys.Shift,
                PluginName = Name,
                Action = _ =>
                {
                    _snippetManager.RemoveSnippet(snippet);
                    _context.API.RemoveUserSelectedItem(selectedResult);
                    return true;
                },
            },
        ];
    }
    private void OnThemeChanged(Theme currentTheme, Theme newTheme)
    {
        UpdateIconPath(newTheme);
    }

    private void UpdateIconPath(Theme theme)
    {
        if (theme == Theme.Light || theme == Theme.HighContrastWhite)
        {
            _iconPath = "Images/Snippets.light.png";
        }
        else
        {
            _iconPath = "Images/Snippets.dark.png";
        }
    }

    public System.Windows.Controls.Control CreateSettingPanel()
        => throw new NotImplementedException();

    public void UpdateSettings(PowerLauncherPluginSettings settings)
    {
        if (settings?.AdditionalOptions is null)
        {
            return;
        }

        var typeDelay = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "PasteDelay");
        _beginTypeDelay = (int)(typeDelay?.NumberValue ?? 200);
    }

    /// <summary>
    /// Start an Action within an STA Thread
    /// </summary>
    /// <param name="action">The action to execute in the STA thread</param>
    static void RunAsSTAThread(Action action)
    {
        AutoResetEvent @event = new AutoResetEvent(false);
        Thread thread = new Thread(
            () =>
            {
                action();
                @event.Set();
            });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        @event.WaitOne();
    }

    private T RunSync<T>(Func<Task<T>> func)
        => Task.Run(func).GetAwaiter().GetResult();

    [DllImport("User32.dll")]
    static extern int SetForegroundWindow(IntPtr point);

    [DllImport("User32.dll")]
    static extern IntPtr GetForegroundWindow();
}
