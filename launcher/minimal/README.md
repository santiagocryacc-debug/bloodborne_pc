# Bloodborne Minimal Launcher

Windows WPF launcher prototype for Awaken Port. English UI, dark brown/gold/deep red theme.

## Build status
The GitHub Actions workflow in this branch compiles the launcher on a Windows runner and uploads a self-contained Windows x64 artifact. The artifact is a test build; keep the original launcher until the new launcher has been tested against the installed port.

## Configuration and launch
- Frame Cap: 30, 60, 90, 120, Uncapped (144 intentionally omitted).
- Reads/writes selected keys in BBPort.ini and preserves comments and unknown keys.
- Makes a timestamped backup before writing.
- Launches through scripts\run_windows.py and passes FPS/presentation settings as environment variables.

## Test checklist
1. Extract the artifact.
2. Run the launcher, select the port folder containing scripts\run_windows.py and out\bb-probe.exe.
3. Select the game folder containing Bloodborne.exe.
4. Verify the displayed INI path is the intended BBPort.ini before saving.
5. Start with existing settings and confirm the game launches. Keep the original launcher available for rollback.
