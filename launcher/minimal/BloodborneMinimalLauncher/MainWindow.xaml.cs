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
        ("Native AA", "0"), ("Quality", "1"), ("Balanced", "2"), ("Performance", "3"), ("Ultra Performance", "4") };
    private static readonly (string Label, string Value)[] FrameCaps = {
        ("30 FPS", "30"), ("60 FPS", "60"), ("90 FPS", "90"), ("120 FPS", "120"), ("Uncapped", "uncap") };
    private static readonly (string Label, string Value)[] PresentModes = {
        ("Mailbox", "Mailbox"), ("FIFO", "Fifo"), ("FIFO Relaxed", "FifoRelaxed"), ("Immediate", "Immediate") };

    public MainWindow()
    {
        InitializeComponent();
        Fill(ResolutionBox, Resolutions);
        Fill(UpscalerBox, Upscalers);
        Fill(PresetBox, Presets);
        Fill(FrameCapBox, FrameCaps);
        Fill(PresentationBox, PresentModes);
        LoadSettings();
        RefreshIni();
    }

    private static void Fill(ComboBox box, (string Label, string Value)[] items)
    {
        box.ItemsSource = items.Select(x => new ComboBoxItem { Content = x.Label, Tag = x.Value }).ToList();
    }

    private static string Selected(ComboBox box, string fallback) =>
        (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;

    private static void Select(ComboBox box, string value, string fallback)
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
            Select(FrameCapBox, settings.Get("frame_cap", "60"), "60");
            Select(PresentationBox, settings.Get("presentation_mode", "Mailbox"), "Mailbox");
        }
        catch { }
    }

    private void SaveSettings()
    {
        var settings = new IniFile(_settingsPath);
        settings.Set("port_dir", _portDir);
        settings.Set("frame_cap", Selected(FrameCapBox, "60"));
        settings.Set("presentation_mode", Selected(PresentationBox, "Mailbox"));
        settings.SaveWithBackup();
    }

    private string FindIniPath()
    {
        foreach (var dir in new[] { _portDir, AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar) }
                     .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            foreach (var name in new[] { "BBPort.ini", "bbport.ini" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return string.IsNullOrWhiteSpace(_portDir) ? "" : Path.Combine(_portDir, "bbport.ini");
    }

    private void RefreshIni()
    {
        try
        {
            var path = FindIniPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _ini = null;
                if (!string.IsNullOrWhiteSpace(_portDir))
                    StatusText.Text = "BBPort.ini not found in the selected folder.";
                return;
            }

            _ini = new IniFile(path);
            Select(ResolutionBox, _ini.Get("output_res", "1920x1080"), "1920x1080");
            Select(UpscalerBox, _ini.Get("upscaler", "fsr4"), "fsr4");
            Select(PresetBox, _ini.Get("preset", "0"), "0");
            FullscreenBox.IsChecked = _ini.Get("fullscreen", "1") == "1";
            HideMouseBox.IsChecked = _ini.Get("hide_cursor", "1") == "1";
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            _ini = null;
            StatusText.Text = "Could not read settings: " + ex.Message;
        }
    }

    private void ChoosePort_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose the folder containing BBPort.ini",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _portDir = dialog.SelectedPath;
            RefreshIni();
            try { SaveSettings(); } catch (Exception ex) { StatusText.Text = ex.Message; }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_portDir))
                throw new InvalidOperationException("Choose the port folder first.");
            if (_ini is null)
                throw new InvalidOperationException("BBPort.ini was not found. Choose the folder containing BBPort.ini.");
            _ini.Set("output_res", Selected(ResolutionBox, "1920x1080"));
            _ini.Set("upscaler", Selected(UpscalerBox, "fsr4"));
            _ini.Set("preset", Selected(PresetBox, "0"));
            _ini.Set("fullscreen", FullscreenBox.IsChecked == true ? "1" : "0");
            _ini.Set("hide_cursor", HideMouseBox.IsChecked == true ? "1" : "0");
            _ini.Set("frame_cap", Selected(FrameCapBox, "60"));
            _ini.Set("present_mode", Selected(PresentationBox, "Mailbox"));
            _ini.SaveWithBackup();
            SaveSettings();
            StatusText.Text = "Settings saved.";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "Bloodborne", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
