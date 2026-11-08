param(
    [Parameter(Mandatory=$true)][string]$OfficialArchive,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-fA-F0-9]{64}$')][string]$OfficialSha256,
    [Parameter(Mandatory=$true)][string]$NativeDirectory,
    [Parameter(Mandatory=$true)][string]$LauncherDirectory,
    [Parameter(Mandatory=$true)][string]$WorkDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$officialHash = $OfficialSha256.ToLowerInvariant()
if ((Get-FileHash -LiteralPath $OfficialArchive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $officialHash) { throw 'The official archive is not the pinned v0.9.39-beta.31 release' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputRoot) { throw 'Output folder already exists; choose a new empty destination' }
$stageRoot = Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ('package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
Expand-Archive -LiteralPath $OfficialArchive -DestinationPath (Join-Path $stageRoot 'official')
$starters = @(Get-ChildItem -LiteralPath (Join-Path $stageRoot 'official') -Recurse -File -Filter bb-start.exe | Where-Object { $_.FullName -match '[\\/]engine[\\/]out[\\/]bb-start\.exe$' })
if ($starters.Count -ne 1) { throw 'Unexpected official package layout' }
$packageRoot = $starters[0].Directory.Parent.Parent.FullName
$officialPackage = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
if ($officialPackage.version -ne '0.9.39-beta.31') { throw 'Unexpected official engine version' }
$nativeOut = Join-Path ([IO.Path]::GetFullPath($NativeDirectory)) 'engine/out'
foreach ($name in @('bb-probe.exe','bb-engine-assets.exe','bb-shader-lab.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $nativeOut $name))) { throw "Missing patched native executable: $name" }
}
if (-not (Test-Path -LiteralPath (Join-Path $LauncherDirectory 'Launcher.exe'))) { throw 'Missing cinematic Launcher.exe' }
$nativeManifest = Join-Path $NativeDirectory 'native-build.json'
if (-not (Test-Path -LiteralPath $nativeManifest)) { throw 'Missing native build provenance' }
$nativeBuild = Get-Content -LiteralPath $nativeManifest -Raw | ConvertFrom-Json
if ($nativeBuild.tag -ne 'v0.9.39-beta.31' -or $nativeBuild.upstreamCommit -ne '008208bea1895647f185303f71b02a53ea915a5d') { throw 'Native build targets a different release' }
$nativeFiles = @(Get-ChildItem -LiteralPath $nativeOut -File)
if (@($nativeBuild.files).Count -ne $nativeFiles.Count) { throw 'Native manifest file count mismatch' }
foreach ($file in $nativeFiles) {
    $entry = @($nativeBuild.files | Where-Object { $_.name -ceq $file.Name })
    if ($entry.Count -ne 1 -or $entry[0].sha256 -cne (Get-FileHash -LiteralPath $file.FullName).Hash.ToLowerInvariant()) { throw "Native manifest hash mismatch: $($file.Name)" }
}
New-Item -ItemType Directory -Path $outputRoot | Out-Null
Copy-Item -Path (Join-Path $packageRoot '*') -Destination $outputRoot -Recurse
foreach ($file in Get-ChildItem -LiteralPath $nativeOut -File) {
    if ($file.Extension -notin @('.exe','.dll')) { throw "Unexpected native runtime file: $($file.Name)" }
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $outputRoot 'engine/out') -Force
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $outputRoot "engine/out/$($file.Name)")).Hash) { throw 'Native copy integrity failure' }
}
python (Join-Path $PSScriptRoot 'runtime-fixes/audit_pe_runtime.py') (Join-Path $outputRoot 'engine/out')
if ($LASTEXITCODE -ne 0) { throw 'Packaged PE runtime compatibility check failed' }
# Exercise the actual helper with Windows error dialogs disabled and a bounded timeout.
python (Join-Path $PSScriptRoot 'runtime-fixes/smoke_shader_helper.py') (Join-Path $outputRoot 'engine/out/bb-shader-lab.exe')
if ($LASTEXITCODE -ne 0) { throw 'Packaged shader helper failed its cache smoke check' }
$smokeRoot = Join-Path $stageRoot 'empty game folder'
New-Item -ItemType Directory -Path $smokeRoot | Out-Null
$env:BB_CONFIG = Join-Path $smokeRoot 'lantern.ini'
$env:BB_DATA_DIR = $smokeRoot
$env:BB_USER_DIR = Join-Path $smokeRoot 'saves'
$env:BB_GPU_USER_DIR = Join-Path $smokeRoot 'user'
$startExe = Join-Path $outputRoot 'engine/out/bb-start.exe'
$startOutput = & $startExe --game-dir $smokeRoot 2>&1
if ($LASTEXITCODE -ne 1 -or ($startOutput -join "`n") -notlike '*No eboot.bin in*') {
    throw "Engine starter failed its command-line smoke check: $startOutput"
}
Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'Launcher.exe') -Destination (Join-Path $outputRoot 'Launcher.exe')
$launcherFile = Get-Item -LiteralPath (Join-Path $outputRoot 'Launcher.exe')
if ($launcherFile.VersionInfo.FileVersion -ne '0.9.39.31') { throw 'Unexpected cinematic launcher version' }
$manifest = [ordered]@{
    tag = 'v0.9.39-beta.31'
    upstreamCommit = '008208bea1895647f185303f71b02a53ea915a5d'
    officialArchiveSha256 = $officialHash
    nativeBuild = $nativeBuild
    nativeOutputResolutionHidden = $true
    shaderLabMatchedRuntime = $true
    hiddenSystemPages = @('PCControls', 'PCKeys', 'PCLighting', 'PCDetail', 'PCLooks')
    launcherSourceCommit = $env:GITHUB_SHA
    launcherSha256 = (Get-FileHash -LiteralPath $launcherFile.FullName).Hash.ToLowerInvariant()
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputRoot 'cinematic-engine.json') -Encoding utf8NoBOM
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UPDATE-039.md') -Destination (Join-Path $outputRoot 'LEEME-ACTUALIZACION.md')
foreach ($document in @('CONSIGNA-FUTURO.md', 'THIRD-PARTY-SAVES.md', 'LICENSE-SAVES.txt')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $document) -Destination (Join-Path $outputRoot $document)
}
$lines = foreach ($file in Get-ChildItem -LiteralPath $outputRoot -Recurse -File | Sort-Object FullName) {
    ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) + '  ' + [IO.Path]::GetRelativePath($outputRoot, $file.FullName).Replace('\','/')
}
$lines | Set-Content -LiteralPath (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding utf8NoBOM
Write-Output "Verified package: $outputRoot"
# The starter intentionally returns 1 for the empty test fixture.
exit 0
