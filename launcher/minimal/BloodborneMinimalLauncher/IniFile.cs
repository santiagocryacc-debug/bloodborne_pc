using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BloodborneMinimalLauncher;

/// <summary>Edits selected INI values while retaining comments, unknown keys and original line order.</summary>
public sealed class IniFile
{
    private readonly List<string> _lines;
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    public string PathName { get; }

    public IniFile(string path)
    {
        PathName = path;
        _lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
        foreach (var line in _lines)
        {
            if (!line.Contains('=') || line.TrimStart().StartsWith('#') || line.TrimStart().StartsWith(';')) continue;
            var at = line.IndexOf('=');
            _values[line[..at].Trim()] = line[(at + 1)..].Trim();
        }
    }

    public string Get(string key, string fallback = "") => _values.TryGetValue(key, out var value) ? value : fallback;
    public void Set(string key, string value) => _values[key] = value;

    public void SaveWithBackup()
    {
        var dir = Path.GetDirectoryName(PathName);
        if (string.IsNullOrWhiteSpace(dir)) throw new InvalidOperationException("The INI path is invalid.");
        Directory.CreateDirectory(dir);
        if (File.Exists(PathName))
        {
            var backup = PathName + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            File.Copy(PathName, backup, false);
        }

        var output = new List<string>();
        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in _lines)
        {
            if (line.Contains('=') && !line.TrimStart().StartsWith('#') && !line.TrimStart().StartsWith(';'))
            {
                var at = line.IndexOf('=');
                var key = line[..at].Trim();
                if (_values.TryGetValue(key, out var value))
                {
                    if (written.Add(key)) output.Add($"{key}={value}");
                    continue;
                }
            }
            output.Add(line);
        }
        foreach (var pair in _values)
            if (written.Add(pair.Key)) output.Add($"{pair.Key}={pair.Value}");
        File.WriteAllText(PathName, string.Join(Environment.NewLine, output) + Environment.NewLine, new UTF8Encoding(false));
    }
}
