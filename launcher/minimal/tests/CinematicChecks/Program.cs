using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BloodborneMinimalLauncher;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        _checks++;
        Console.WriteLine("PASS " + name);
    }

    private static Dictionary<string, FrameworkElement> Controls(MainWindow window, string name) =>
        (Dictionary<string, FrameworkElement>)typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;

    private static void Select(ComboBox box, string value) =>
        box.SelectedItem = box.Items.Cast<ComboBoxItem>().Single(item => (string)item.Tag == value);

    private static void Render(MainWindow window, string path, int tab, bool bottom = false, int width = 1550, int height = 920)
    {
        ((TabControl)window.FindName("SettingsTabs")).SelectedIndex = tab;
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        static IEnumerable<DependencyObject> Descendants(DependencyObject node)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
        foreach (var scroll in Descendants(content).OfType<ScrollViewer>().Where(v => v.ScrollableHeight > 0))
            scroll.ScrollToVerticalOffset(bottom ? scroll.ScrollableHeight : 0);
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(root);
            var package = Path.Combine(root, "fixture");
            var engineOut = Path.Combine(package, "engine", "out");
            Directory.CreateDirectory(engineOut);
            var starter = Path.Combine(engineOut, "bb-start.exe");
            File.WriteAllText(starter, "test fixture only");
            File.WriteAllText(Path.Combine(package, "cinematic-engine.json"), JsonSerializer.Serialize(new { tag = EngineRelease.Tag, upstreamCommit = GraphicsOptions.UpstreamCommit }));
            var path = Path.Combine(package, "lantern.ini");
            File.WriteAllText(path, "# Existing configuration\nupscaler=fsr4\npreset=5\nrender_scale=1.75\ndlss_preset=K\ncustom_future_key=keep-me\nframe_gen_cap=1\n");
            var settings = Path.Combine(root, "settings.ini");
            File.WriteAllText(settings, "# Isolated test preferences\n");
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            var window = new MainWindow(settings, starter);
            ComboBox Box(string name) => (ComboBox)window.FindName(name);
            var graphics = Controls(window, "_graphicsControls");
            var engine = Controls(window, "_engineControls");
            Check((string)((ComboBoxItem)Box("UpscalerBox").SelectedItem).Tag == "fsr", "Legacy FSR choice migrates in the UI");
            Check(new IniFile(path).Get("upscaler") == "fsr4", "Opening does not rewrite the engine configuration");
            Check(Box("UpscalerBox").Items.Cast<ComboBoxItem>().Any(i => (string)i.Tag == "xess"), "XeSS is selectable");
            Check(graphics["render_scale"].IsEnabled, "Custom render ratio is enabled for Custom");
            Select(Box("PresetBox"), "1");
            Check(!graphics["render_scale"].IsEnabled, "Custom render ratio is disabled for fixed presets");
            Select(Box("UpscalerBox"), "dlss");
            Check(((CheckBox)window.FindName("DlssJitterFadeBox")).IsEnabled, "DLSS controls follow the upscaler");
            Select(Box("UpscalerBox"), "xess");
            Check(!((CheckBox)window.FindName("DlssJitterFadeBox")).IsEnabled, "XeSS disables DLSS-only controls");
            Select(Box("UpscalerBox"), "dlss");
            Select(Box("FrameGenerationBox"), "dlss");
            Check(Box("GeneratedFramesBox").IsEnabled && Box("ReflexBox").IsEnabled, "DLSS FG dependencies remain available");
            Check(GraphicsOptions.Lighting.Where(o => o.Key.StartsWith("effect_")).All(o => ((CheckBox)graphics[o.Key]).IsChecked == false), "New lighting effects default to off");
            ((CheckBox)graphics["effect_ray_tracing"]).IsChecked = true;
            ((CheckBox)graphics["effect_contact_shadows"]).IsChecked = true;
            ((Slider)graphics["graphics_gi_strength"]).Value = 7.25;
            ((Slider)graphics["mip_bias"]).Value = -0.5;
            Select((ComboBox)graphics["graphics_shadow_resolution"], "2");
            ((CheckBox)engine["LANTERN_LIFTER"]).IsChecked = false;
            ((CheckBox)window.FindName("DlssJitterFadeBox")).IsChecked = true;
            window.SaveCurrentSettings();
            var saved = new IniFile(path);
            Check(saved.Get("effect_ray_tracing") == "1" && saved.Get("effect_contact_shadows") == "1", "Lighting switches persist to the real engine keys");
            Check(saved.Get("graphics_gi_strength") == "7.25" && saved.Get("mip_bias") == "-0.50", "Numeric values use decimal points under Spanish culture");
            Check(saved.Get("graphics_shadow_resolution") == "2", "Shadow resolution uses the engine index");
            Check(saved.Get("dlss_preset") == "K" && saved.Get("custom_future_key") == "keep-me", "DLSS K and unknown options survive saving");
            Check(File.ReadAllText(path).Contains("# Existing configuration"), "INI comments survive saving");
            Check(saved.Get("frame_gen_cap") == "0", "Removed Half Refresh Rate remains off");
            Check(new IniFile(settings).Get("LANTERN_LIFTER") == "0", "Native renderer choices persist separately");
            var info = new ProcessStartInfo();
            info.Environment["LANTERN_JOB_SLOTS"] = "0";
            window.ApplyNewEngineOptions(info);
            Check(info.Environment["LANTERN_LIFTER"] == "0" && info.Environment["LANTERN_JOB_SLOTS"] == "1", "Engine switches override inherited values in the child only");
            var before = File.ReadAllText(path);
            Select(Box("UpscalerBox"), "off");
            var rejected = false;
            try { window.SaveCurrentSettings(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected && File.ReadAllText(path) == before, "Invalid Frame Generation combination never writes the INI");
            Select(Box("UpscalerBox"), "dlss");
            Render(window, Path.Combine(root, "graphics-top.png"), 1);
            Render(window, Path.Combine(root, "graphics-bottom.png"), 1, true);
            Render(window, Path.Combine(root, "effects-top.png"), 2);
            Render(window, Path.Combine(root, "effects-bottom.png"), 2, true);
            Render(window, Path.Combine(root, "effects-small.png"), 2, true, 1280, 820);
            Render(window, Path.Combine(root, "fixes.png"), 4);
            window.Close();
            var reopened = new MainWindow(settings, starter);
            Check(((CheckBox)Controls(reopened, "_graphicsControls")["effect_ray_tracing"]).IsChecked == true, "Lighting settings reload after reopening");
            Check(((CheckBox)Controls(reopened, "_engineControls")["LANTERN_LIFTER"]).IsChecked == false, "Native renderer settings reload after reopening");
            reopened.Close();
            Check(EngineRelease.IsCompatible(package), "Pinned package marker is recognized");
            File.WriteAllText(Path.Combine(package, "cinematic-engine.json"), "{\"tag\":\"v0.9.37\",\"upstreamCommit\":\"old\"}");
            Check(!EngineRelease.IsCompatible(package), "An old engine package is rejected");
            var oldPort = Path.Combine(root, "old-port");
            var newPort = Path.Combine(root, "migration-port");
            Directory.CreateDirectory(oldPort);
            Directory.CreateDirectory(Path.Combine(newPort, "engine", "out"));
            var oldIni = Path.Combine(oldPort, "lantern.ini");
            File.WriteAllText(oldIni, "# Previous installation\nupscaler=dlss\ndlss_preset=K\n");
            File.WriteAllText(settings, "port_dir=" + oldPort + "\n");
            var migrated = new MainWindow(settings, Path.Combine(newPort, "engine", "out", "bb-start.exe"));
            Check(File.ReadAllText(Path.Combine(newPort, "lantern.ini")) == File.ReadAllText(oldIni), "First start copies the previous configuration without changing it");
            migrated.Close();
            app.Shutdown();
            Console.WriteLine($"Completed {_checks} checks; previews: {root}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
