# Lantern Engine 0.9.37 beta 8 patch

Run `hide_ingame_output_resolution.py` against the `engine/` directory from Awakenchan/LanternEngine tag `v0.9.37-beta.8` only. The script validates five exact source matches before touching files.

The patch hides only the Output Resolution dropdown from the native System > PC Graphics menu, adjusts the row count, and increments the menu asset generator version. All other native System menus, including Log In, remain unchanged.

Build the native source using `engine/build.sh` with Windows MSYS2 CLANG64. Replace only the official beta 8 `engine/out/bb-probe.exe` and `engine/out/bb-engine-assets.exe` with the matching patched artifacts. Do not mix old beta 4 or beta 9.8 binaries.

Use a separate test folder and preserve all game files, settings and saves. CI success does not prove in-game compatibility; test the menu and camera before using this version as the daily driver.

## Validation before installation

The beta 8 integration workflow must produce successful `launcher`, `engine` and `package` jobs. Check build logs and package integrity before installing anything into a separate test folder. The stable install and saved games stay untouched.
