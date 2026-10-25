#!/usr/bin/env python3
"""Verify the patch against real Lantern sources; optionally compile a C++ smoke test."""
from pathlib import Path
import argparse
import importlib.util
import math
import re
import subprocess
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("ultra_quality_85", HERE / "ultra_quality_85.py")
patch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(patch)


def verify(root: Path, cxx: str | None = None) -> None:
    launcher = (HERE.parent / "BloodborneMinimalLauncher/MainWindow.xaml.cs").read_text(encoding="utf-8")
    ultra_scale = re.search(r'UltraQualityScale = "([0-9.]+)"', launcher).group(1)
    originals = {}
    for relative, before, after in patch.RULES:
        text = (root / relative).read_text(encoding="utf-8")
        if text.count(after) == 1:
            text = text.replace(after, before, 1)
        if text.count(before) != 1:
            raise RuntimeError(f"Unexpected upstream fixture: {relative}")
        originals[relative] = text

    class PatchTests(unittest.TestCase):
        def staged(self, folder):
            for relative, text in originals.items():
                path = folder / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(text, encoding="utf-8")

        def test_exact_release(self):
            with tempfile.TemporaryDirectory(dir=root.parent) as tmp:
                folder = Path(tmp)
                self.staged(folder)
                patch.apply(folder)
                for relative, before, after in patch.RULES:
                    self.assertEqual((folder / relative).read_text(encoding="utf-8"),
                                     originals[relative].replace(before, after, 1))

        def test_second_file_mismatch_writes_nothing(self):
            with tempfile.TemporaryDirectory(dir=root.parent) as tmp:
                folder = Path(tmp)
                self.staged(folder)
                relative, before, _ = patch.RULES[1]
                (folder / relative).write_text(originals[relative].replace(before, "// changed upstream"),
                                              encoding="utf-8")
                snapshot = {p: (folder / p).read_bytes() for p in originals}
                with self.assertRaises(RuntimeError):
                    patch.apply(folder)
                self.assertEqual(snapshot, {p: (folder / p).read_bytes() for p in originals})

        def test_reapplying_refuses_without_writes(self):
            with tempfile.TemporaryDirectory(dir=root.parent) as tmp:
                folder = Path(tmp)
                self.staged(folder)
                patch.apply(folder)
                snapshot = {p: (folder / p).read_bytes() for p in originals}
                with self.assertRaises(RuntimeError):
                    patch.apply(folder)
                self.assertEqual(snapshot, {p: (folder / p).read_bytes() for p in originals})

        def test_85_percent_survives_six_decimal_save(self):
            scale = float(format(float(ultra_scale), ".6f"))
            self.assertAlmostEqual(1 / scale, 0.85, places=6)
            for output, expected in (((1920, 1080), (1632, 916)),
                                     ((2560, 1440), (2176, 1224)),
                                     ((3840, 2160), (3264, 1836))):
                actual = tuple(math.floor(side / scale / 4 + 0.5) * 4 for side in output)
                self.assertEqual(actual, expected)

    result = unittest.TextTestRunner(verbosity=2).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(PatchTests))
    if not result.wasSuccessful():
        raise SystemExit(1)

    if not cxx:
        return
    with tempfile.TemporaryDirectory(dir=root.parent) as tmp:
        folder = Path(tmp)
        dlss_path, before, after = patch.RULES[1]
        dlss = originals[dlss_path].replace(before, after, 1)
        quality = re.search(r"int Quality\(int preset\) \{.*?\n\}", dlss, re.S).group(0)
        settings_path, before, after = patch.RULES[0]
        settings = originals[settings_path].replace(before, after, 1)
        save_scale = re.search(r'put\("render_scale", fixed\(v.render_scale, (\d+)\)\)', settings)
        digits = int(save_scale.group(1))
        header = (root / "gpu/shadps4/video_core/renderer_vulkan/scene_resolution.h").as_posix()
        harness = """#include <cassert>
#include <cstdio>
#include <cstdlib>
#include <cmath>
#include "HEADER"
namespace BbSettings { enum { NativeAA, Quality, Balanced, Performance, UltraPerformance }; }
enum { Dlaa = 6, MaxQuality = 2, Balanced = 1, MaxPerf = 0, UltraPerformance = 3 };
QUALITY
int main() {
    char saved[32];
    std::snprintf(saved, sizeof(saved), "%.*f", DIGITS, double(float(ULTRA_SCALE)));
    const double scale = std::strtod(saved, nullptr);
    assert(std::abs(1.0 / scale - 0.85) < 0.000001);
    using Vulkan::SceneResolution::ForScale;
    using Vulkan::SceneResolution::Size;
    assert((ForScale(scale, {1920, 1080}) == Size{1632, 916}));
    assert((ForScale(scale, {2560, 1440}) == Size{2176, 1224}));
    assert((ForScale(scale, {3840, 2160}) == Size{3264, 1836}));
    assert((ForScale(1.0, {3840, 2160}) == Size{3840, 2160}));
    assert((ForScale(1.5, {3840, 2160}) == Size{2560, 1440}));
    assert((ForScale(1.7, {3840, 2160}) == Size{2260, 1272}));
    assert((ForScale(2.0, {3840, 2160}) == Size{1920, 1080}));
    assert((ForScale(3.0, {3840, 2160}) == Size{1280, 720}));
    assert(Quality(BbSettings::NativeAA) == MaxQuality);
    assert(Quality(BbSettings::Quality) == MaxQuality);
    assert(Quality(BbSettings::Balanced) == Balanced);
    assert(Quality(BbSettings::Performance) == MaxPerf);
    assert(Quality(BbSettings::UltraPerformance) == UltraPerformance);
}
""".replace("HEADER", header).replace("QUALITY", quality).replace("DIGITS", str(digits)).replace("ULTRA_SCALE", ultra_scale)
        source = folder / "verify.cpp"
        binary = folder / "verify.exe"
        source.write_text(harness, encoding="utf-8")
        subprocess.run([cxx, "-std=c++20", "-Wall", "-Wextra", "-Werror", str(source), "-o", str(binary)], check=True)
        subprocess.run([str(binary)], check=True)
        print("Compiled C++ checks passed: exact scene sizes, scale persistence and DLSS quality mapping")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("engine_root", type=Path)
    parser.add_argument("--cxx")
    args = parser.parse_args()
    verify(args.engine_root.resolve(), args.cxx)
