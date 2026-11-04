using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace BloodborneMinimalLauncher;

public partial class MainWindow
{
    private const string UltraQualityPreset = "ultra_quality";
    private const double UltraQualityRatio = 1.0 / 0.85;

    private void UpdateUltraQualityAvailability()
    {
        if (PresetBox is null) return;
        var dlss = Selected(UpscalerBox, "off") == "dlss";
        if (!dlss && Selected(PresetBox, "1") == UltraQualityPreset)
            Select(PresetBox, "1", "1");
        foreach (ComboBoxItem item in PresetBox.Items)
        {
            if (item.Tag?.ToString() != UltraQualityPreset) continue;
            item.IsEnabled = dlss;
            item.Visibility = dlss ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void LoadUpscalingPreset(IniFile ini)
    {
        var preset = ini.Get("preset", "1");
        if (Selected(UpscalerBox, "off") == "dlss" && IsUltraQualityConfiguration(ini))
            preset = UltraQualityPreset;
        Select(PresetBox, preset, "1");
        UpdateUltraQualityAvailability();
    }

    private static bool IsUltraQualityConfiguration(IniFile ini) =>
        ini.Get("upscaler") == "dlss" && ini.Get("preset") == "5" &&
        ini.Get("cinematic_preset") == UltraQualityPreset &&
        double.TryParse(ini.Get("render_scale"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio) &&
        // Native System saves serialize render_scale to two decimals; keep the named preset recognizable.
        System.Math.Abs(ratio - UltraQualityRatio) < 0.005;

    internal static void ApplyUltraQualityOverride(ProcessStartInfo info, IniFile ini)
    {
        if (!IsUltraQualityConfiguration(ini)) return;
        info.Environment["BB_UPSCALER"] = "dlss";
        info.Environment["BB_PRESET"] = "5";
        info.Environment["BB_RENDER_SCALE"] = UltraQualityRatio.ToString("R", CultureInfo.InvariantCulture);
    }

    private void SaveUpscalingPreset(IniFile ini)
    {
        var preset = Selected(PresetBox, "1");
        if (preset == UltraQualityPreset)
        {
            if (Selected(UpscalerBox, "off") != "dlss")
                throw new System.InvalidOperationException("Ultra Quality Requires DLSS");
            // Use the engine's supported Custom mode, with an exact fixed 85% ratio.
            // This override follows slider serialization so two-decimal rounding cannot alter it.
            ini.Set("preset", "5");
            ini.Set("render_scale", UltraQualityRatio.ToString("R", CultureInfo.InvariantCulture));
            ini.Set("cinematic_preset", UltraQualityPreset);
        }
        else
        {
            ini.Set("preset", preset);
            ini.Set("cinematic_preset", "");
        }
    }
}
