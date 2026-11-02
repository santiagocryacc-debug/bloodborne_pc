using System;
using System.IO;
using System.Text.Json;

namespace BloodborneMinimalLauncher;

internal static class EngineRelease
{
    internal const string Tag = "v0.9.39-beta.29";
    internal const string OfficialSha256 = "e208370d0f7db38c91f5b9169cca8c57eee8a1f3f7621ac9756a0c9f65d83c85";

    internal static bool IsCompatible(string root)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "cinematic-engine.json")));
            return json.RootElement.GetProperty("tag").GetString() == Tag &&
                json.RootElement.GetProperty("upstreamCommit").GetString() == GraphicsOptions.UpstreamCommit;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
        {
            return false;
        }
    }
}
