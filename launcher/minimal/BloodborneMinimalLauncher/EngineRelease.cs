using System;
using System.IO;
using System.Text.Json;

namespace BloodborneMinimalLauncher;

internal static class EngineRelease
{
    internal const string Tag = "v0.9.39-beta.31";
    internal const string UpstreamCommit = "008208bea1895647f185303f71b02a53ea915a5d";

    internal static bool IsCompatible(string root)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "cinematic-engine.json")));
            var tag = json.RootElement.GetProperty("tag").GetString();
            var commit = json.RootElement.GetProperty("upstreamCommit").GetString();
            return (tag, commit) is (Tag, UpstreamCommit) or
                ("v0.9.39-beta.29", "e6b943dd969d99561545315f3d801f10e4a6cf8d");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
        {
            return false;
        }
    }
}
