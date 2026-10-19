using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace BloodborneMinimalLauncher;

public partial class App : Application
{
    public App()
    {
        // This is a lightweight configuration editor, not a game. Avoid WPF's
        // Direct3D rendering path, which can be picked up by G-SYNC/VRR and
        // cause low idle frame rates and OLED brightness flicker.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
    }
}
