# Bloodborne Cinematic Launcher

WPF .NET 8 launcher replacing the original Bloodborne Launcher.exe for the verified local `bb-start.exe` -> `bb-probe.exe` flow.

## Interface

- Left navigation: PLAY, GRAPHICS, FOLDERS, DISPLAY, EFFECTS, BACKUP
- Right panel: full-height GAME SETTINGS with dark tabs and no internal scroll bars at the default 1550 x 920 window size
- Decorative news cards, bottom status bar, slogans and duplicated captions removed
- Tab hover and selection use dark crimson with light gold text
- Preserves the working settings and game/saves folder selectors
- Hero artwork is **optional** at `Assets/Yharnam.jpg` next to the published Launcher.exe; if missing, the app starts without the image
- Artwork: image control uses high-quality bitmap scaling. To avoid stray letters from the original artwork, use a cropped hero image without embedded UI text

## Backup

The BACKUP button copies all files and subfolders from:

`Y:\Games\Library\Standalone\Bloodborne\Install\Game Files\Saves\savedata\1\CUSA00207\SPRJ0005`

to a new, timestamped folder under:

`Y:\Games\Library\Standalone\Bloodborne\Install\Save Editor\backups`

A backup is named `SPRJ0005_yyyy-MM-dd_HH-mm-ss`, with a numeric suffix if required. Existing backups are never overwritten. Files are **copied**, never moved or deleted. The game must be closed; the launcher checks for `bb-probe.exe` before beginning a copy. Failures are displayed, and any partial destination should be inspected before retrying.

The backup always uses the specific source path above, not the currently selected alternative Saves Folder profile.

## Deploy and test

1. Download the latest successful `BloodborneMinimalLauncher-win-x64` artifact from GitHub Actions.
2. Copy `Launcher.exe` to the port root beside `engine/` and `bbport.ini`.
3. To display the cinematic background, place `Yharnam.jpg` in `Assets/` alongside the launcher executable.
4. Confirm the DISPLAY, GRAPHICS, EFFECTS and FOLDERS tabs show the saved values and have legible hover/selection states.
5. With Bloodborne closed, press BACKUP and verify the timestamped destination contains the expected files.
6. Launch and verify `bb-start.exe` and `bb-probe.exe` are running without the default launcher; verify your selected save profile before saving.

The image crop supplied with the chat is sharpened and upscaled, but cannot restore true detail beyond its original generated resolution.

## Safety

- No writes to the game's extracted PKG folder by the new launcher, apart from the engine's own established behavior.
- Backup source files are read-only from this feature.
- The prior working cinematic state is preserved on branch `launcher-cinematic-pre-cleanup`.
- The classic minimal launcher remains on `launcher-classic-backup`.
