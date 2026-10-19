using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BloodborneMinimalLauncher;

public partial class MainWindow : Window
{
    private string _portDir = "";
    private IniFile? _ini;
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BloodborneMinimalLauncher", "settings.ini");

    private static readonly (string Label, string Value)[] Resolutions = {
        ("1280 720", "1280x720"), ("1920 1080", "1920x1080"), ("2560 1440", "2560x1440"),
        ("3840 2160", "3840x2160"), ("2560 1080", "2560x1080"), ("3440 1440", "3440x1440"),
        ("3840 1600", "3840x1600"), ("5120 2160", "5120x2160"), ("3840 1080", "3840x1080"),
        ("5120 1440", "5120x1440"), ("7680 2160", "7680x2160"), ("Auto", "auto") };
    private static readonly (string Label, string Value)[] Upscalers = {
        ("FSR 4", "fsr4"), ("FSR 4.1.1", "fsr411"), ("FSR 3", "fsr3"),
        ("DLSS", "dlss"), ("TAA", "taa"), ("Off", "off") };
    private static readonly (string Label, string Value)[] Presets = {
        ("Native", "0"), ("Quality", "1"), ("Balanced", "2"), ("Performance", "3"), ("Ultra Performance", "4") };
    private static readonly (string Label, string Value)[] FrameCaps = {
        ("30 FPS", "30"), ("60 FPS", "60"), ("90 FPS", "90"), ("120 FPS", "120"), ("Uncapped", "uncap") };
    private static readonly (string Label, string Value)[] PresentModes = {
        ("Mailbox", "Mailbox"), ("FIFO", "Fifo"), ("FIFO Relaxed", "FifoRelaxed"), ("Immediate", "Immediate") };
    private static readonly (string Label, string Value)[] LiveResolutionModes = {
        ("Auto", "auto"), ("Off", "0"), ("On", "1") };
    private static readonly (string Label, string Value)[] ModelLods = {
        ("As In Game", "0"), ("Highest", "-2"), ("Lower", "1"), ("Lowest", "2") };

    public MainWindow()
    {
        InitializeComponent();
        Fill(ResolutionBox, Resolutions);
        Fill(UpscalerBox, Upscalers);
        Fill(PresetBox, Presets);
        Fill(FrameCapBox, FrameCaps);
        Fill(PresentationBox, PresentModes);
        Fill(LiveResolutionBox, LiveResolutionModes);
        Fill(ModelLodBox, ModelLods);
        LoadSettings();
        RefreshIni();
    }

    private static void Fill(System.Windows.Controls.ComboBox box, (string Label, string Value)[] items)
    {
        box.ItemsSource = items.Select(x => new ComboBoxItem { Content = x.Label, Tag = x.Value }).ToList();
    }

    private static string Selected(System.Windows.Controls.ComboBox box, string fallback) =>
        (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;

    private static void Select(System.Windows.Controls.ComboBox box, string value, string fallback)
    {
        foreach (ComboBoxItem item in box.Items)
            if (Equals(item.Tag?.ToString(), value)) { box.SelectedItem = item; return; }
        foreach (ComboBoxItem item in box.Items)
            if (Equals(item.Tag?.ToString(), fallback)) { box.SelectedItem = item; return; }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var settings = new IniFile(_settingsPath);
            _portDir = settings.Get("port_dir");
            Select(FrameCapBox, settings.Get("fps_mode", "60"), "60");
            Select(PresentationBox, settings.Get("present_mode", "Mailbox"), "Mailbox");
        }
        catch { }
    }

    private void SaveSettings()
    {
        var settings = new IniFile(_settingsPath);
        settings.Set("port_dir", _portDir);
        settings.Set("fps_mode", Selected(FrameCapBox, "60"));
        settings.Set("present_mode", Selected(PresentationBox, "Mailbox"));
        settings.SaveWithBackup();
    }

    private string FindIniPath()
    {
        var candidates = new System.Collections.Generic.List<string>();
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; current is not null && depth < 5; depth++, current = current.Parent)
            candidates.Add(current.FullName);
        candidates.Add(Environment.CurrentDirectory);
        if (!string.IsNullOrWhiteSpace(_portDir))
            candidates.Add(_portDir);

        foreach (var dir in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var name in new[] { "BBPort.ini", "bbport.ini" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    _portDir = dir;
                    return candidate;
                }
            }
        }

        _portDir = "";
        return "";
    }

    private void RefreshIni()
    {
        try
        {
            var path = FindIniPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _ini = null;
                StatusText.Text = "BBPort.ini not found. Place this launcher in the game folder, beside blobborne.exe.";
                return;
            }

            _ini = new IniFile(path);
            Select(ResolutionBox, _ini.Get("output_res", "1920x1080"), "1920x1080");
            Select(FrameCapBox, _ini.Get("fps_mode", "60"), "60");
            Select(PresentationBox, _ini.Get("present_mode", "Mailbox"), "Mailbox");
            Select(UpscalerBox, _ini.Get("upscaler", "fsr4"), "fsr4");
            Select(PresetBox, _ini.Get("preset", "1"), "1");
            Select(ModelLodBox, _ini.Get("model_lod", "0"), "0");
            FullscreenBox.IsChecked = _ini.Get("fullscreen", "1") == "1";
            HideMouseBox.IsChecked = _ini.Get("hide_cursor", "1") == "1";
            Select(LiveResolutionBox, _ini.Get("live_resolution", "auto"), "auto");
            SharpenBox.IsChecked = _ini.Get("sharpen", "1") == "1";
            SharpnessSlider.Value = double.TryParse(_ini.Get("sharpness", "0.50"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sharpness) ? Math.Clamp(sharpness, 0, 2) : 0.5;
            SharpnessValueText.Text = SharpnessSlider.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            ObjectMotionBox.IsChecked = _ini.Get("object_motion", "1") == "1";
            SsaoBox.IsChecked = _ini.Get("effect_ssao", "1") == "1";
            DynamicShadowsBox.IsChecked = _ini.Get("effect_dynamic_shadows", "1") == "1";
            ChromaticAberrationBox.IsChecked = _ini.Get("effect_chromatic_aberration", "1") == "1";
            DepthOfFieldBox.IsChecked = _ini.Get("effect_dof", "1") == "1";
            MotionBlurBox.IsChecked = _ini.Get("effect_motion_blur", "1") == "1";
            GameAaBox.IsChecked = _ini.Get("effect_game_aa", "1") == "1";
            ScreenSpaceReflectionsBox.IsChecked = _ini.Get("effect_ssr", "0") == "1";
            SkipIntroBox.IsChecked = _ini.Get("skip_intro", "0") == "1";
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            _ini = null;
            StatusText.Text = "Could not read settings: " + ex.Message;
        }
    }

    private void SharpnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SharpnessValueText is not null)
            SharpnessValueText.Text = e.NewValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshIni();
            if (_ini is null)
                throw new InvalidOperationException("BBPort.ini was not found. Place this launcher in the game folder, beside blobborne.exe.");
            _ini.Set("output_res", Selected(ResolutionBox, "1920x1080"));
            _ini.Set("upscaler", Selected(UpscalerBox, "fsr4"));
            _ini.Set("preset", Selected(PresetBox, "1"));
            _ini.Set("model_lod", Selected(ModelLodBox, "0"));
            _ini.Set("fullscreen", FullscreenBox.IsChecked == true ? "1" : "0");
            _ini.Set("hide_cursor", HideMouseBox.IsChecked == true ? "1" : "0");
            _ini.Set("live_resolution", Selected(LiveResolutionBox, "auto"));
            _ini.Set("sharpen", SharpenBox.IsChecked == true ? "1" : "0");
            _ini.Set("sharpness", SharpnessSlider.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            _ini.Set("object_motion", ObjectMotionBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_ssao", SsaoBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_dynamic_shadows", DynamicShadowsBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_chromatic_aberration", ChromaticAberrationBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_dof", DepthOfFieldBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_motion_blur", MotionBlurBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_game_aa", GameAaBox.IsChecked == true ? "1" : "0");
            _ini.Set("effect_ssr", ScreenSpaceReflectionsBox.IsChecked == true ? "1" : "0");
            _ini.Set("skip_intro", SkipIntroBox.IsChecked == true ? "1" : "0");
            _ini.Set("fps_mode", Selected(FrameCapBox, "60"));
            _ini.Set("present_mode", Selected(PresentationBox, "Mailbox"));
            _ini.SaveWithBackup();
            SaveSettings();
            StatusText.Text = "Settings Saved.";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Bloodborne", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
