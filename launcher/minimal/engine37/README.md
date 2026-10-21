# Lantern Engine 0.9.37 beta 4 — Output Resolution hidden

This is an **isolated engine update**. Do not mix its binaries with the old
0.9.34 beta 9.8 package.

Native engine source is based on the pinned
[Lantern Engine v0.9.37-beta.4](https://github.com/Awakenchan/LanternEngine/releases/tag/v0.9.37-beta.4)
and the original v0.4.0 preview. The workflow applies the exact upstream patch
then the local `hide_ingame_output_resolution.py` patch.

Changes in `System > PC Graphics`:
- Remove only the semi-transparent Output Resolution dropdown.
- Five visible rows, no invisible or selectable sixth slot.
- Preserve the resolution setting in `lantern.ini` and the cinematic launcher.
- Preserve upstream engine bug fixes from versions 0.9.34 to 0.9.37 beta 4.

**Important:** the source script does not mutate `lantern.ini`, save files or
game assets. The native compile generates only replacement `bb-probe.exe` and
`bb-engine-assets.exe`. Those files must be overlaid onto a complete,
separate **official 0.9.37 beta 4** package; original upstream files provide
the other matching executables, shaders, DLLs, and content.

The launcher build for this branch uses `lantern.ini` (with safe migration
from local `bbport.ini`) and keeps user's graphics preferences separate.

Testing required in game: resolution from DISPLAY should work; native in-game
PC Graphics should show all settings except Output Resolution; entering and
exiting the menu should not cause crashes or hidden focus rows.
