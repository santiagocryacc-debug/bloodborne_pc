using System;
using System.Collections.Generic;

namespace BloodborneMinimalLauncher;

// Keys, defaults and bounds from the supported Lantern engine settings.
internal sealed record GraphicOption(string Key, string Label, string Default,
    double? Min = null, double? Max = null, (string Label, string Value)[]? Choices = null);

internal static class GraphicsOptions
{

    internal static string NormalizeUpscaler(string value) => value is "fsr3" or "fsr4" ? "fsr" : value;

    internal static readonly GraphicOption[] Lighting = {
        new("effect_modern_ssr", "Improved Reflections", "0"),
        new("effect_pom_cavity", "Deeper Parallax Surfaces", "0"),
        new("effect_contact_shadows", "Contact Shadows", "0"),
        new("effect_pcss", "Soft Sun Shadows", "0"),
        new("effect_gi", "Indirect Lighting", "0"),
        new("effect_volumetric_light", "Volumetric Sunlight", "0"),
        new("effect_point_fog", "Fog Around Lamps", "0"),
        new("effect_gtao", "GTAO", "0"),
        new("effect_point_shadows", "Point Light Shadows", "0"),
        new("effect_ray_tracing", "Ray Traced Lighting", "0"),
        new("graphics_shadow_resolution", "Sun Shadow Resolution", "0", Choices: new[] { ("Game's Own", "0"), ("4096", "1"), ("8192", "2") }),
        new("graphics_local_shadow_resolution", "Lamp Shadow Resolution", "0", Choices: new[] { ("Game's Own", "0"), ("High", "1"), ("Very High", "2") }),
        new("graphics_motion_blur_quality", "Motion Blur Quality", "0", Choices: new[] { ("Game's Own", "0"), ("High", "1") }),
        new("graphics_gi_strength", "Indirect Light Strength", "6", 0, 12),
        new("graphics_ssr_intensity", "Reflection Strength", "1", 0, 3),
        new("graphics_volumetric_intensity", "Sunlight Strength", "1", 0, 3),
        new("graphics_point_fog_intensity", "Lamp Fog Strength", "1", 0, 3),
    };

    internal static readonly GraphicOption[] Upscaling = {
        new("render_scale", "Custom Render Ratio", "1.3", 1, 3),
        new("mip_bias", "Texture MIP Bias", "0", -2, 1),
    };

    internal static readonly GraphicOption[] NativeEngine = {
        new("LANTERN_GX_DRAWS", "Native Renderer Draws", "1"),
        new("LANTERN_GX_TEXTURES", "Native Renderer Textures", "1"),
        new("LANTERN_GX_BUFFERS", "Native Renderer Buffers", "1"),
        new("LANTERN_GX_RECORDS", "Native Renderer Pipelines", "1"),
        new("LANTERN_READ_FILTER", "Native Renderer Read Tracking", "1"),
        new("LANTERN_LIFTER", "Native Shader Compiler", "1"),
    };

    internal static readonly GraphicOption[] Fixes = {
        new("LANTERN_FRAMERATE", "Native Frame Rate", "1"),
        new("LANTERN_FRAMERATE_FOLLOW", "Steps Follow Frame Rate", "1"),
        new("LANTERN_JOB_SLOTS", "Cutscene Freeze Fix", "1"),
        new("LANTERN_MEMORY_RETRY_LIMIT", "Memory Retry Limit", "1"),
    };
}
