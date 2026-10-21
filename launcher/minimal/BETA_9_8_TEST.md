# Lantern Engine beta 9.8 compatibility test

The goal is to test the existing cinematic `Launcher.exe` with the complete
`v0.9.34-beta.9.8` Lantern Engine package without replacing the known-good installation.

## Important boundaries

- **Do not** replace only `bb-probe.exe` or individual DLLs: use the complete
  official package because the engine, shader compiler, scripts, assets and patches
  change together.
- **Do not** touch the unpacked Bloodborne game directory.
- Keep `launcher-cinematic-pre-cleanup` / `minimal-launcher-build` and the
  current `Test Port` installation intact.
- The generated `Launcher.exe` contains only the UI and launch integration.
  It **does not** bundle or update the third-party engine.
- Our launcher still invokes `engine/out/bb-start.exe` and never needs
  `Bloodborne Launcher.exe`.
- This branch is experimental. A successful .NET compile is **not** proof that
  the updated engine starts or that the settings work in-game.

## Side-by-side installation on Windows

1. Close all running Bloodborne / bb-probe / bb-start processes.
2. Download the official
   [LanternEngine-v0.9.34-beta.9.8-win64.zip](https://github.com/Awakenchan/LanternEngine/releases/download/v0.9.34-beta.9.8/LanternEngine-v0.9.34-beta.9.8-win64.zip).
3. Extract the **entire** archive into a **new directory**, e.g.
   `Y:\Games\Library\Standalone\Bloodborne\Test Port Beta 9.8`.
   Make sure the path contains `engine\out\bb-start.exe`.
4. Copy only the newly built `Launcher.exe` from this branch's GitHub Actions
   artifact into the new package root. Keep any official executable untouched.
5. To retain the artwork, copy the existing `Assets\Yharnam.jpg` next to
   the new `Launcher.exe`, inside `Assets`.
6. The new package needs its **own** `bbport.ini` in the package root.
   If the official package has not created one, **copy** the previous
   `bbport.ini` into this new package root; never move it. The new launcher
   prioritizes this local copy and passes `BB_CONFIG` and `BB_DATA_DIR` to
   the engine. This keeps the two installations independent.
7. Open the new `Launcher.exe`. Under FOLDERS, set GAME FOLDER to the existing
   game-files directory and SAVES FOLDER to your current existing saves
   directory. Save settings. This does not copy or move your game files or saves.
8. For the first run use Frame Generation = Off and keep your current known-good
   display settings. Verify the process chain `Launcher.exe -> bb-start.exe ->
   bb-probe.exe`, loading the same save and that the game is stable.
9. Next test GRAPHICS: Sharp UI On at 1440p with native AA / DLAA.
10. Last test Frame Generation = FSR 3.1 x2 with an active upscaler, then
    Half Refresh Rate. Expect a brief first-time hitch and some latency.
    Frame Generation is disabled with menus, videos and HDR.

## Confirm settings

Frame Generation is written into the new package's `bbport.ini`:

```ini
frame_gen=fsr3
frame_gen_cap=1
```

Sharp UI is a process environment switch, not an INI key.
The checked checkbox passes `BB_SHARP_UI=1` to `bb-start.exe`;
unchecked leaves the variable unset, matching the upstream launcher.

The current beta also has VRAM / shader / new-engine optimizations.
We intentionally leave their engine defaults in place until we validate
the basic migration and agree on the visual controls.

## Troubleshooting

- If the window opens but the game has no saves, check that
  `bb-probe.exe` actually has `--user` pointing at the selected SAVES FOLDER.
- If the engine starts without Frame Generation, confirm the full 9.8 engine,
  the exact active `bbport.ini`, and that an upscaler is on.
- Check the launcher log at
  `%LOCALAPPDATA%\BloodborneMinimalLauncher\last_launch.log`
  and the new package's engine logs.
- Revert immediately to the old standalone Test Port directory if new-engine
  testing is unstable. Do not mix binaries between the two folders.
