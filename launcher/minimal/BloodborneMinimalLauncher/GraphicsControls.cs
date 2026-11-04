using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using HAlign = System.Windows.HorizontalAlignment;

namespace BloodborneMinimalLauncher;

public partial class MainWindow
{
    private readonly Dictionary<string, FrameworkElement> _graphicsControls = new();
    private readonly Dictionary<string, FrameworkElement> _engineControls = new();

    private void BuildNewGraphicsControls()
    {
        AddOptions(UpscalingOptionsPanel, GraphicsOptions.Upscaling, _graphicsControls);
        AddOptions(LightingOptionsPanel, GraphicsOptions.Lighting, _graphicsControls);
        AddOptions(NativeEngineOptionsPanel, GraphicsOptions.NativeEngine, _engineControls);
        AddOptions(NewFixesPanel, GraphicsOptions.Fixes, _engineControls);
        LoadNewEngineOptions(new IniFile(""));
        EngineVersionText.Text = "Lantern " + GraphicsOptions.Version.Replace(" beta ", " Beta ");
        PresetBox.SelectionChanged += (_, _) => UpdateCustomRenderRatio();
        UpdateCustomRenderRatio();
    }

    private void UpdateCustomRenderRatio() => _graphicsControls["render_scale"].IsEnabled = Selected(PresetBox, "1") == "5";

    private void AddOptions(StackPanel panel, IEnumerable<GraphicOption> options, Dictionary<string, FrameworkElement> controls)
    {
        foreach (var option in options)
        {
            FrameworkElement control;
            if (option.Choices is not null)
            {
                var combo = new ComboBox { Width = 160, HorizontalAlignment = HAlign.Right };
                Fill(combo, option.Choices);
                control = combo;
            }
            else if (option.Min is not null)
            {
                var slider = new Slider { Minimum = option.Min.Value, Maximum = option.Max!.Value,
                    Width = 150, TickFrequency = 0.05, IsSnapToTickEnabled = true };
                control = slider;
            }
            else
            {
                var toggle = new CheckBox { Content = option.Label, Margin = new Thickness(0, 0, 0, 8),
                    HorizontalAlignment = HAlign.Left };
                controls.Add(option.Key, toggle);
                panel.Children.Add(toggle);
                continue;
            }
            controls.Add(option.Key, control);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168) });
            row.Children.Add(new TextBlock { Text = option.Label, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            Grid.SetColumn(control, 1);
            if (control is Slider numeric)
            {
                var box = new StackPanel();
                Grid.SetColumn(box, 1);
                box.Children.Add(numeric);
                var value = new TextBlock { HorizontalAlignment = HAlign.Right };
                value.Text = numeric.Value.ToString("0.00", CultureInfo.InvariantCulture);
                numeric.ValueChanged += (_, _) => value.Text = numeric.Value.ToString("0.00", CultureInfo.InvariantCulture);
                box.Children.Add(value);
                row.Children.Add(box);
            }
            else row.Children.Add(control);
            panel.Children.Add(row);
        }
    }

    private static void LoadOptions(IniFile ini, IEnumerable<GraphicOption> options, Dictionary<string, FrameworkElement> controls)
    {
        foreach (var option in options)
        {
            var value = ini.Get(option.Key, option.Default);
            switch (controls[option.Key])
            {
                case CheckBox toggle: toggle.IsChecked = value == "1"; break;
                case ComboBox combo: Select(combo, value, option.Default); break;
                case Slider slider:
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount) || !double.IsFinite(amount))
                        amount = double.Parse(option.Default, CultureInfo.InvariantCulture);
                    slider.Value = Math.Clamp(amount, slider.Minimum, slider.Maximum);
                    break;
            }
        }
    }

    private static string OptionValue(FrameworkElement control) => control switch
    {
        CheckBox toggle => toggle.IsChecked == true ? "1" : "0",
        ComboBox combo => Selected(combo, "0"),
        Slider slider => slider.Value.ToString("0.00", CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException("Unknown graphics control")
    };

    private void LoadNewEngineOptions(IniFile settings) =>
        LoadOptions(settings, GraphicsOptions.NativeEngine.Concat(GraphicsOptions.Fixes), _engineControls);

    private void SaveNewEngineOptions(IniFile settings)
    {
        foreach (var (key, control) in _engineControls) settings.Set(key, OptionValue(control));
    }

    private void LoadNewGraphicsOptions(IniFile ini)
    {
        LoadOptions(ini, GraphicsOptions.Lighting.Concat(GraphicsOptions.Upscaling), _graphicsControls);
        DlssJitterFadeBox.IsChecked = ini.Get("dlss_jitter_fade", "0") == "1";
        UpscaleBeforePostBox.IsChecked = ini.Get("upscale_before_post", "0") == "1";
        AmdAntiLagBox.IsChecked = ini.Get("anti_lag", "1") == "1";
    }

    private void SaveNewGraphicsOptions(IniFile ini)
    {
        foreach (var (key, control) in _graphicsControls) ini.Set(key, OptionValue(control));
        ini.Set("dlss_jitter_fade", DlssJitterFadeBox.IsChecked == true ? "1" : "0");
        ini.Set("upscale_before_post", UpscaleBeforePostBox.IsChecked == true ? "1" : "0");
        ini.Set("anti_lag", AmdAntiLagBox.IsChecked == true ? "1" : "0");
        SaveUpscalingPreset(ini);
    }

    internal void ApplyNewEngineOptions(ProcessStartInfo info)
    {
        foreach (var (key, control) in _engineControls) info.Environment[key] = OptionValue(control);
    }
}
