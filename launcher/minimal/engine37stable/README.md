# Bloodborne Cinematic — Lantern Engine 0.9.37 stable integration

Pinned upstream official release: https://github.com/Awakenchan/LanternEngine/releases/tag/v0.9.37
Pinned source commit: `d901fbd2ea4bea114b37c26a2a9278d1e97f7b46`

This branch inherits the beta 8 WPF launcher changes (Frame Generation Off/FSR 3.1/DLSS, generated frames 2x–6x and Reflex) and adds NVIDIA RTX 50 dynamic DLSS frame generation via `frame_gen_dynamic=0/1`.

Native patch: `hide_ingame_output_resolution.py` targets the entirely rewritten `engine/gpu/shim/engine/option_menu.cpp` of **stable** 0.9.37 and its new `engine/tools/engine/menu_assets.cpp`. It removes just the Output Resolution pick row, adjusts the fog binding from index 5 to 4, removes the obsolete pick-caption helper, and changes PCGraphics' generated menu layout from six rows to five (plus Defaults). It deliberately does not change saved `output_res`, the in-game System section, native Log In, or any game data.

The full build workflow `.github/workflows/build-lantern-stable-cinematic.yml` checks out the exact official source commit, applies the strict source-only patch, compiles the Windows native engine and the .NET 8 cinematic launcher, and packages the exact official stable ZIP with two patched native files + our Launcher.exe. It does **not** use old beta build artifacts.

**Not yet tested or ready to install** until GitHub Actions has completed successfully and the native output menu, camera, DLSS and saves are checked in a separate installation. Our previous beta 4 and beta 9.8 branches remain untouched.

The official upstream stable build (NOT our customized build): https://github.com/Awakenchan/LanternEngine/releases/download/v0.9.37/LanternEngine-v0.9.37-win64.zip
