// Copyright (c) Corey Hayward. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Community.PowerToys.Run.Plugin.Snippets.Models;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace Community.PowerToys.Run.Plugin.Snippets;
public class SnippetManager
{
    private const string FileName = "snippets.json";
    private string SnippetsPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, FileName);
    private List<Snippet> _snippets = [];

    public IReadOnlyList<Snippet> Snippets => _snippets.AsReadOnly();

    public SnippetManager()
    {
        if (!File.Exists(SnippetsPath))
        {
            return;
        }

        var json = File.ReadAllText(SnippetsPath);
        _snippets = JsonConvert.DeserializeObject<List<Snippet>>(json) ?? [];
    }

    public void AddSnippet(Snippet snippet)
    {
        _snippets.Add(snippet);
        SaveSnippetsToFile();
    }

    public void RemoveSnippet(Snippet snippet)
    {
        _snippets.Remove(snippet);
        SaveSnippetsToFile();
    }

    private void SaveSnippetsToFile()
    {
        var json = JsonConvert.SerializeObject(_snippets, Formatting.Indented);
        File.WriteAllText(SnippetsPath, json);
    }
}
