# Bloodborne Cinematic Launcher

WPF .NET 8 with Lantern v0.9.39-beta.29.
Read ../UPDATE-039.md for current scope and deployment.
Read AGENTS.md and ../CONSIGNA-FUTURO.md before changing interface text.

Navigation: Play, Graphics, Folders, Display, Effects, Fixes, Edit Save, Backup.
Edit Save opens only characters from the same active save root used by BB_USER_DIR.
The original main Backup button retains its fixed source and destination. No profile manager, import or restore controls are exposed.
Editing retains transactional writes and an internal safety copy. Real test fixtures must never be published.

Build: dotnet publish BloodborneMinimalLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
Checks: dotnet run --project ../tests/CinematicChecks/CinematicChecks.csproj -c Release -- validation
