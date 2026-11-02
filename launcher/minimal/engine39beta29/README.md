# Cinematic integration: Lantern Engine v0.9.39-beta.29

Pinned upstream commit: `e6b943dd969d99561545315f3d801f10e4a6cf8d`.
Official Windows archive SHA256: `e208370d0f7db38c91f5b9169cca8c57eee8a1f3f7621ac9756a0c9f65d83c85`.

`hide_ingame_output_resolution.py` verifies both source hashes before editing.
It removes only Output Resolution from System > PC Graphics, changes the menu
layout from six settings to five, and binds Fog to its new index. New PC Lighting,
PC Lighting Detail and PC Looks menus remain intact. The Insert/L3+R3 overlay
continues to be disabled by the cinematic launcher.

The native build is performed by `.github/workflows/build-lantern-039-beta29-native.yml`.
It rebuilds `bb-probe.exe` and `bb-engine-assets.exe` and collects their full DLL
dependency closure from the same compiler environment. All other engine files,
the FSR SDK, fsr4vk, NVIDIA runtimes and the data used by the engine come from the
exact official archive. No binaries from 0.9.37 are reused.

`test_patch.py <upstream checkout>` tests patch isolation, the version guards and
failure without partial writes. Build success is distinct from a real game test.

The cinematic launcher exposes the new graphics settings and native renderer
switches. Online play, save editing and the official launcher's other interfaces
are outside this update's scope. XeSS is selectable but the official release
does not contain `libxess.dll`; the engine falls back to FSR 3.1 without it.
