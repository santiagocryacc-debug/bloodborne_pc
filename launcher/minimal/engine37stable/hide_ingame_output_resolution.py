#!/usr/bin/env python3
"""Hide only the in-game Output Resolution row in Lantern Engine v0.9.37 stable.

Runs against the engine/ directory of the official release source.
Exact-match, all-or-nothing staging; refuses to touch unexpected future versions.
This does not modify DISPLAY, native System menus, game data or saves.
"""
from pathlib import Path
import argparse

RULES = [
    ("const Choice kOutputs[] ...", "const Choice kOutputs[] = {{0, 117010},  {1, 117011},  {2, 117012},  {3, 117013},  {4, 117015},\n                           {5, 117040},  {6, 117041},  {7, 117042},  {8, 117043},  {9, 117044},\n                           {10, 117045}, {11, 117046}, {12, 117047}, {13, 117048}};\nstatic_assert(std::size(kOutputs) == BbSettings::OutputCount);\n", ""),
    ("graphics row", "Setting g_graphics[] = {Toggle(117001, 0, true),           Toggle(117000, 1, true),\n                        Slider(117007, 5, 0, 0.2f, true),  Slider(117006, 0, 1, 0.2f, true),\n                        Pick(117004, BbSettings::OutputDefault, kOutputs), Slider(117008, 10, 0, 0.1f, true)};\n", "Setting g_graphics[] = {Toggle(117001, 0, true),          Toggle(117000, 1, true),\n                        Slider(117007, 5, 0, 0.2f, true), Slider(117006, 0, 1, 0.2f, true),\n                        Slider(117008, 10, 0, 0.1f, true)};\n"),
    ("old graphics pick caption index", "constexpr int kGraphicsPick = 4;\n", ""),
    ("graphics caption helpers", "struct DefaultsCaption {\n    std::uint8_t* dialog = nullptr;\n    std::uint8_t* choices = nullptr;\n    char16_t text[32]{};\n    char16_t first = 0;\n};\nDefaultsCaption g_caption;\n\nvoid KeepDefaultsCaption(std::uint8_t* dialog) {\n    g_caption = DefaultsCaption{};\n    const int rows = OptionRows::RowCount(dialog);\n    if (rows != int(std::size(g_graphics)) + 1) return;\n    const char16_t* label = OptionRows::RowLabel(dialog, rows - 1);\n    if (!label || label != OptionRows::Message(OptionRows::kDefaultsRow)) return;\n    std::size_t n = 0;\n    for (; label[n] && n + 1 < std::size(g_caption.text); ++n) g_caption.text[n] = label[n];\n    OptionRows::SetRowLabel(dialog, rows - 1, g_caption.text);\n    OptionRows::Layout(dialog);\n    g_caption.dialog = dialog;\n    g_caption.choices = OptionRows::PickChoices(OptionRows::RowWidget(dialog, kGraphicsPick));\n}\n\nvoid UpdateDefaultsCaption(std::uint8_t* list, bool focused) {\n    DefaultsCaption& c = g_caption;\n    if (!c.dialog || !focused) return;\n    if (list == c.choices && !c.first && c.text[0]) {\n        c.first = c.text[0];\n        c.text[0] = 0;\n        OptionRows::Redraw(OptionRows::RowsList(c.dialog));\n    } else if (list == OptionRows::RowsList(c.dialog) && c.first) {\n        c.text[0] = c.first;\n        c.first = 0;\n        OptionRows::Redraw(list);\n    }\n}\n\n", ""),
    ("open section resets old caption", "    g_caption = DefaultsCaption{};\n", ""),
    ("open section extra caption handling", "    } else if (d && S == kGraphics) {\n        KeepDefaultsCaption(d);\n    }\n", "    }\n"),
    ("graphics bound value", "    g_graphics[4].number = &s.output_res;\n    g_graphics[5].amount = &s.graphics_fog;\n", "    g_graphics[4].amount = &s.graphics_fog;\n"),
    ("unused caption update", "    UpdateDefaultsCaption(list, focused);\n", ""),
]
GEN_BEFORE = "{\"PCGraphics\", 117020, 6, true}"
GEN_AFTER = "{\"PCGraphics\", 117020, 5, true}"

def apply(root: Path) -> None:
    paths = [root / "gpu/shim/engine/option_menu.cpp", root / "tools/engine/menu_assets.cpp"]
    if any(not p.is_file() for p in paths):
        raise RuntimeError("Official Lantern v0.9.37 engine source files are missing")
    native = paths[0].read_text(encoding="utf-8")
    gen = paths[1].read_text(encoding="utf-8")
    # Stage every edit in RAM, and abort if any exact match is missing or ambiguous.
    for label, old, new in RULES:
        count = native.count(old)
        if count != 1:
            raise RuntimeError(f"Expected one {label} source pattern; found {count}. Nothing changed.")
        native = native.replace(old, new, 1)
    if gen.count(GEN_BEFORE) != 1:
        raise RuntimeError("Native PCGraphics movie layout has changed. Nothing changed.")
    gen = gen.replace(GEN_BEFORE, GEN_AFTER, 1)
    if "Pick(117004" in native or "g_caption" in native:
        raise RuntimeError("Resolution row/caption cleanup is incomplete. Nothing changed.")
    if 'g_graphics[4].amount = &s.graphics_fog;' not in native:
        raise RuntimeError("Fog slider binding missing. Nothing changed.")
    # Write both only after all checks pass.
    paths[0].write_text(native, encoding="utf-8")
    paths[1].write_text(gen, encoding="utf-8")
    print("Stable 0.9.37 native Output Resolution row removed; remaining System menu preserved")

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("engine_root", type=Path)
    args = p.parse_args()
    apply(args.engine_root.resolve())
