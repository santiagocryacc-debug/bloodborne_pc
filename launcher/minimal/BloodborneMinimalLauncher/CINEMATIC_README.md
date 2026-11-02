# Bloodborne Cinematic Launcher

WPF .NET 8. Current engine: Lantern v0.9.39-beta.29.
Read ../UPDATE-039.md for deployment and save operations.
Read AGENTS.md and ../CONSIGNA-FUTURO.md before changing any interface text.

Navigation: Play, Graphics, Folders, Display, Effects, Fixes, Saves, Backup.
Optional artwork: Assets/Yharnam.jpg. Existing artwork and configuration are preserved.
Save Manager and Save Editor open separate dark windows with concise English labels.

The main Backup button always uses SaveRepository.FixedRoot and FixedBackups.
Profile backups, imports, restores and edits go through SaveRepository. Never write raw save
files directly from a UI handler. Tests use synthetic files and optional private copies;
real fixtures must never be committed or included in a source handoff.

Build and verify:
- Restore Bloodborne.ico from Bloodborne.ico.b64.
- dotnet publish BloodborneMinimalLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
- dotnet run --project ../tests/CinematicChecks/CinematicChecks.csproj -c Release -- validation
- ../Build-Cinematic039.ps1 assembles the pinned engine and custom launcher.
