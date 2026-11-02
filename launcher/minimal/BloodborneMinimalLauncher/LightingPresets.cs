using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;

namespace BloodborneMinimalLauncher;

public partial class MainWindow
{
    private Dictionary<string, string>? _pendingLook;

    // Matches gpu/shim/bbport_looks.cpp in the pinned engine. Applying is explicit;
    // the INI is still written only by SAVE SETTINGS.
    internal void ApplyLightingPreset(string preset)
    {
        var values = new Dictionary<string, string>
        {
            ["graphics_controls"]="0", ["graphics_vignette"]="1", ["graphics_brightness"]="1",
            ["graphics_contrast"]="1", ["graphics_saturation"]="1", ["graphics_hue"]="0",
            ["graphics_bloom"]="1", ["graphics_fog"]="1", ["graphics_ao_strength"]="1", ["graphics_shadow_scale"]="1",
            ["effect_chromatic_aberration"]="0", ["effect_dof"]="0", ["effect_motion_blur"]="0",
            ["effect_ssao"]="1", ["effect_game_aa"]="0", ["effect_dynamic_shadows"]="1", ["effect_ssr"]="0"
        };
        foreach (var option in GraphicsOptions.Lighting) values[option.Key] = option.Default;
        switch (preset)
        {
            case "original": break;
            case "performance":
                values["graphics_controls"]="1";
                values["graphics_shadow_scale"]="0.75";
                values["effect_dynamic_shadows"]="0";
                break;
            case "shadows":
                values["effect_contact_shadows"]="1";
                values["effect_pcss"]="1";
                break;
            case "remastered":
                foreach (var key in new[] { "effect_ssr", "effect_modern_ssr", "effect_pom_cavity", "effect_contact_shadows",
                    "effect_pcss", "effect_gi", "effect_volumetric_light", "effect_point_fog", "graphics_shadow_resolution", "graphics_local_shadow_resolution" })
                    values[key]="1";
                break;
            default: throw new ArgumentException("Unknown lighting preset", nameof(preset));
        }
        _pendingLook = values;
        var originalEffects = new Dictionary<string, CheckBox>
        {
            ["graphics_controls"]=PictureTuningBox, ["effect_chromatic_aberration"]=ChromaticAberrationBox,
            ["effect_dof"]=DepthOfFieldBox, ["effect_motion_blur"]=MotionBlurBox, ["effect_ssao"]=SsaoBox,
            ["effect_game_aa"]=GameAaBox, ["effect_dynamic_shadows"]=DynamicShadowsBox, ["effect_ssr"]=ScreenSpaceReflectionsBox
        };
        foreach (var (key, box) in originalEffects) box.IsChecked = values[key] == "1";
        foreach (var option in GraphicsOptions.Lighting)
        {
            switch (_graphicsControls[option.Key])
            {
                case CheckBox toggle: toggle.IsChecked = values[option.Key] == "1"; break;
                case ComboBox combo: Select(combo, values[option.Key], option.Default); break;
                case Slider slider: slider.Value = double.Parse(values[option.Key], CultureInfo.InvariantCulture); break;
            }
        }
        StatusText.Text = "Preset applied to controls. Click SAVE SETTINGS to keep it.";
    }

    private void ApplyLightingPreset_Click(object sender, RoutedEventArgs e) =>
        ApplyLightingPreset(Selected(LightingPresetBox, "original"));

    private void ApplyPendingLookValues(IniFile ini)
    {
        if (_pendingLook is null) return;
        foreach (var (key, value) in _pendingLook) ini.Set(key, value);
    }
}
