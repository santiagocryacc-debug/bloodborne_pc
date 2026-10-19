using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BloodborneMinimalLauncher;

public partial class MainWindow : Window
{
    private string _portDir = "";
    private string _gameDir = "";
    private IniFile? _ini;
    private readonly string _settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BloodborneMinimalLauncher", "settings.ini");

    private static readonly (string Label, string Value)[] Resolutions = {
        ("1280x720", "1280x720"), ("1920x1080", "1920x1080"), ("2560x1440", "2560x1440"),
        ("3840x2160", "3840x2160"), ("2560x1080", "2560x1080"), ("3440x1440", "3440x1440"),
        ("3840x1600", "3840x1600"), ("5120x2160", "5120x2160"), ("3840x1080", "3840x1080"),
        ("5120x1440", "5120x1440"), ("7680x2160", "7680x2160"), ("Display (auto)", "auto") };
    private static readonly (string Label, string Value)[] Upscalers = {
        ("FSR 4", "fsr4"), ("FSR 4.1.1", "fsr411"), ("FSR 3", "fsr3"),
        ("DLSS", "dlss"), ("TAA", "taa"), ("Off", "off") };
    private static readonly (string Label, string Value)[] Presets = {
        ("Native AA", "0"), ("Quality", "1"), ("Balanced", "2"), ("Performance", "3"), ("Ultra Performance", "4") };
    private static readonly (string Label, string Value)[] FrameCaps = {
        ("30 FPS", "30"), ("60 FPS", "60"), ("90 FPS", "90"), ("120 FPS", "120"), ("Uncapped", "uncap") };
    private static readonly (string Label, string Value)[] PresentModes = {
        ("Mailbox", "Mailbox"), ("FIFO (VSync)", "Fifo"), ("FIFO Relaxed", "FifoRelaxed"), ("Immediate", "Immediate") };

    public MainWindow()
    {
        InitializeComponent();
        Fill(ResolutionBox, Resolutions); Fill(UpscalerBox, Upscalers); Fill(PresetBox, Presets);
        Fill(FrameCapBox, FrameCaps); Fill(PresentationBox, PresentModes);
        LoadSettings();
        RefreshIni();
    }

    private static void Fill(ComboBox box, (string Label, string Value)[] items)
    {
        box.ItemsSource = items.Select(x => new ComboBoxItem { Content = x.Label, Tag = x.Value }).ToList();
    }
    private static string Selected(ComboBox box, string fallback) => (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;
    private static void Select(ComboBox box, string value, string fallback)
    {
        foreach (ComboBoxItem item in box.Items) if (Equals(item.Tag?.ToString(), value)) { box.SelectedItem = item; return; }
        foreach (ComboBoxItem item in box.Items) if (Equals(item.Tag?.ToString(), fallback)) { box.SelectedItem = item; return; }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var settings = new IniFile(_settingsPath);
            _portDir = settings.Get("port_dir"); _gameDir = settings.Get("game_dir");
            Select(FrameCapBox, settings.Get("frame_cap", "60"), "60");
            Select(PresentationBox, settings.Get("presentation_mode", "Mailbox"), "Mailbox");
        }
        catch { }
    }
    private void SaveSettings()
    {
        var settings = new IniFile(_settingsPath);
        settings.Set("port_dir", _portDir); settings.Set("game_dir", _gameDir);
        settings.Set("frame_cap", Selected(FrameCapBox, "60"));
        settings.Set("presentation_mode", Selected(PresentationBox, "Mailbox"));
        settings.SaveWithBackup();
    }

    private string FindIniPath()
    {
        foreach (var dir in new[] { _portDir, _gameDir, AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar) }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            foreach (var name in new[] { "BBPort.ini", "bbport.ini" })
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return Path.Combine(_portDir, "bbport.ini");
    }
    private void RefreshIni()
    {
        try
        {
            var path = FindIniPath();
            _ini = new IniFile(path);
            Select(ResolutionBox, _ini.Get("output_res", "1920x1080"), "1920x1080");
            Select(UpscalerBox, _ini.Get("upscaler", "fsr4"), "fsr4");
            Select(PresetBox, _ini.Get("preset", "0"), "0");
            FullscreenBox.IsChecked = _ini.Get("fullscreen", "1") == "1";
            HideMouseBox.IsChecked = _ini.Get("hide_cursor", "1") == "1";
            PathText.Text = path;
        }
        catch (Exception ex) { StatusText.Text = "Could not read settings: " + ex.Message; }
    }

    private void ChoosePort_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose the Bloodborne PC port folder" };
        if (dialog.ShowDialog() == true) { _portDir = dialog.FolderName; RefreshIni(); UpdatePaths(); }
    }
    private void ChooseGame_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose the folder containing Bloodborne.exe" };
        if (dialog.ShowDialog() == true) { _gameDir = dialog.FolderName; RefreshIni(); UpdatePaths(); }
    }
    private void UpdatePaths() => PathText.Text = $"Port: {_portDir}\nGame: {_gameDir}";

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_portDir) || !File.Exists(Path.Combine(_portDir, "scripts", "run_windows.py")) || !File.Exists(Path.Combine(_portDir, "out", "bb-probe.exe")))
                throw new InvalidOperationException("Choose the port folder containing scripts\\run_windows.py and out\\bb-probe.exe.");
            if (string.IsNullOrWhiteSpace(_gameDir) || !File.Exists(Path.Combine(_gameDir, "Bloodborne.exe")))
                throw new InvalidOperationException("Choose the game folder containing Bloodborne.exe.");
            if (_ini is null) RefreshIni();
            if (_ini is null) throw new InvalidOperationException("The INI file could not be loaded.");

            _ini.Set("output_res", Selected(ResolutionBox, "1920x1080"));
            _ini.Set("upscaler", Selected(UpscalerBox, "fsr4"));
            _ini.Set("preset", Selected(PresetBox, "0"));
            _ini.Set("fullscreen", FullscreenBox.IsChecked == true ? "1" : "0");
            _ini.Set("hide_cursor", HideMouseBox.IsChecked == true ? "1" : "0");
            _ini.SaveWithBackup();
            SaveSettings();

            var python = Path.Combine(_portDir, "tools", "python", "python.exe");
            var useBundled = File.Exists(python);
            var psi = new ProcessStartInfo(useBundled ? python : "py") { WorkingDirectory = _portDir, UseShellExecute = false };
            if (!useBundled) psi.ArgumentList.Add("-3");
            psi.ArgumentList.Add(Path.Combine(_portDir, "scripts", "run_windows.py"));
            psi.ArgumentList.Add("--game-dir"); psi.ArgumentList.Add(_gameDir);
            psi.Environment["BB_PREBUILT"] = "1";
            psi.Environment["BB_DATA_DIR"] = _portDir;
            psi.Environment["BB_CONFIG"] = _ini.PathName;
            psi.Environment["BB_GAME_DIR"] = _gameDir;
            psi.Environment["BB_FPS"] = Selected(FrameCapBox, "60");
            var fps = Selected(FrameCapBox, "60");
            psi.Environment["BB_FPS_LIMIT"] = fps == "uncap" ? "0" : fps;
            psi.Environment["BB_VBLANK_HZ"] = fps == "90" ? "90" : fps is "30" or "60" ? "60" : "0";
            psi.Environment["BB_PRESENT_MODE"] = Selected(PresentationBox, "Mailbox");
            psi.Environment["PYTHONUNBUFFERED"] = "1";
            var userDir = Path.Combine(_portDir, "user");
            psi.Environment["BB_USER_DIR"] = userDir;
            psi.Environment["BB_MODS_DIR"] = Path.Combine(_portDir, "mods");
            psi.Environment["BB_MODS_CONFIG"] = Path.Combine(_portDir, "mods.json");
            psi.Environment["BB_MODS_ENABLED"] = "1";
            psi.Environment["BB_PATCHES_DIR"] = Path.Combine(_portDir, "patches");
            psi.Environment["BB_PATCHES_CONFIG"] = Path.Combine(_portDir, "patches.json");
            psi.Environment["BB_LANGUAGE"] = "1";
            Process.Start(psi);
            StatusText.Text = "Launch started.";
        }
        catch (Exception ex) { StatusText.Text = ex.Message; MessageBox.Show(this, ex.Message, "Bloodborne", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
