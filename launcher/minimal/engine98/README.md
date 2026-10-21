# Remove in-game Output Resolution (Lantern Engine beta 9.8)

**Status: source change prepared, not compiled into the beta engine yet.**

The user wants to hide ONLY `System > PC Graphics > Output Resolution`
because its translucent dropdown overlaps and shows other text. This is
a change to the C++ engine's native game menu, **not** to our WPF launcher.

The official Lantern 0.9.34 beta 9.8 builds its engine by applying
`engine/bbhost-changes.patch` to `DarkIzuku/bloodborne_pc` at
`v0.4.0-darkizuku-preview.1`. After that patch is applied, use:

```sh
python hide_ingame_output_resolution.py /path/to/engine-source
```

The script checks **all five exact beta 9.8 source strings** before
writing anything. It changes:

1. `gpu/shim/engine/option_menu.cpp`: remove the translucent resolution
   row constructor, reduce PCGraphics's row count from 6 to 5, and stop
   labeling row 4 as a dropdown in the Defaults handler.
2. `tools/engine/menu_assets.cpp`: generate a five-row PC Graphics page,
   hide its unused sixth visual slot, and bump menu asset generator 9 -> 10
   to invalidate previously generated files.

It does **not** delete `output_res` from the settings model and never
changes the WPF launcher's `Resolution` selector. The existing
`bbport.ini` remains the source of truth; PC Effects, PC Colour, Display,
and the other PC Graphics rows are unchanged.

A new Windows engine build **including freshly generated menu assets**
must be produced and tested in the separate beta 9.8 test install before
this can affect the game. Replacing `Launcher.exe` alone cannot do it.

Verification checklist:
- In game, `System > PC Graphics` shows AA, SSAO, AO strength, shadow
  distance and fog, but **no Output Resolution row** and no empty
  selectable row.
- In our WPF launcher, `DISPLAY > Resolution` remains functional;
  the chosen `output_res` persists in the beta install's `bbport.ini`.
- No rendering regression when reopening System or PC Graphics; existing
  saves and the previous stable port remain untouched.

Sources:
- https://github.com/Awakenchan/LanternEngine/tree/v0.9.34-beta.9.8
- https://github.com/droogie/bbhost/blob/master/src/engine/option_menu.cpp
