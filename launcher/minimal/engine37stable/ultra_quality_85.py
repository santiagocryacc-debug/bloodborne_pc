#!/usr/bin/env python3
"""Patch exact Lantern v0.9.37 sources for the launcher's 85% Custom preset.

Preserve render_scale precision across native menu saves. Native-sized input is
still DLAA in Record(); a reduced Custom scene near native uses DLSS MaxQuality.
"""
from pathlib import Path
import argparse

RULES = (
    (
        "gpu/shim/bbport_settings.cpp",
        '    put("render_scale", fixed(v.render_scale, 2));',
        '    put("render_scale", fixed(v.render_scale, 6));',
    ),
    (
        "gpu/shadps4/video_core/renderer_vulkan/vk_dlss.cpp",
        "    case BbSettings::NativeAA:\n        return Dlaa;\n    case BbSettings::Quality:\n        return MaxQuality;",
        "    // Native-sized input is selected as DLAA in Record(). A reduced Custom\n"
        "    // scene nearest to NativeAA (including 85%) still needs Super Resolution.\n"
        "    case BbSettings::NativeAA:\n    case BbSettings::Quality:\n        return MaxQuality;",
    ),
)


def apply(root: Path) -> None:
    staged = []
    for relative, before, after in RULES:
        path = root / relative
        text = path.read_text(encoding="utf-8")
        count = text.count(before)
        if count != 1:
            raise RuntimeError(f"Expected one source pattern in {relative}; found {count}. Nothing changed.")
        staged.append((path, text.replace(before, after, 1)))
    # Validate every source before writing either file.
    for path, text in staged:
        path.write_text(text, encoding="utf-8")
    print("Ultra Quality 85%: render scale precision and DLSS Super Resolution mapping patched")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("engine_root", type=Path)
    apply(parser.parse_args().engine_root.resolve())
