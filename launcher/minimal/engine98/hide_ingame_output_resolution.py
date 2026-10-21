#!/usr/bin/env python3
"""Hide ONLY System > PC Graphics > Output Resolution in Lantern Engine 0.9.34 beta 9.8.

Run on the ENGINE SOURCE TREE after applying Awakenchan/LanternEngine's
engine/bbhost-changes.patch. This does not modify the released .exe.

This intentionally does not touch BbSettings::output_res, bbport.ini, or the
launcher resolution selector. Other in-game PC Graphics controls remain.
"""
from __future__ import annotations

import argparse
from pathlib import Path

RULES = {
    "gpu/shim/engine/option_menu.cpp": (
        (
            "    add_list_row(dialog,&output_resolution,117004,outputs,BbSettings::OutputCount,BbSettings::OutputDefault);",
            "    // Output Resolution is deliberately launcher-only. Do not add this dropdown.",
        ),
        (
            '    graphics_seeded=true;log_rows("PCGraphics",dialog,6,6);',
            '    graphics_seeded=true;log_rows("PCGraphics",dialog,5,5);',
        ),
        (
            '    return open_named(root,params,"PCGraphics",reinterpret_cast<void*>(&GraphicsHandler),4);',
            '    return open_named(root,params,"PCGraphics",reinterpret_cast<void*>(&GraphicsHandler),-1);',
        ),
    ),
    "tools/engine/menu_assets.cpp": (
        (
            '    section("PCGraphics", 117020, 384, 6, 0, {});',
            '    section("PCGraphics", 117020, 384, 5, 0, {"Item_5_0"});',
        ),
        (
            "constexpr int kGeneratorVersion = 9;",
            "constexpr int kGeneratorVersion = 10;",
        ),
    ),
}


def apply(source: Path) -> None:
    staged: list[tuple[Path, bytes]] = []
    for relative, rules in RULES.items():
        path = source / relative
        if not path.is_file():
            raise RuntimeError(f"Missing Lantern beta 9.8 source: {path}")
        original = path.read_bytes()
        new_data = original
        for before, after in rules:
            old, new = before.encode("utf-8"), after.encode("utf-8")
            count = new_data.count(old)
            if count != 1:
                raise RuntimeError(
                    f"Expected exactly one beta 9.8 match in {relative}: "
                    f"{before!r}; found {count}. No files were changed."
                )
            new_data = new_data.replace(old, new, 1)
        staged.append((path, new_data))

    # Validate every expected source location before writing any of them.
    for path, data in staged:
        path.write_bytes(data)
        print(f"Patched: {path}")
    print("Output Resolution removed from PC Graphics source only.")
    print("Rebuild the Lantern engine AND regenerate its menu assets to use this change.")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("engine_source_root", type=Path, help="Source root after applying Lantern 9.8 engine patch")
    args = parser.parse_args()
    apply(args.engine_source_root.resolve())


if __name__ == "__main__":
    main()
