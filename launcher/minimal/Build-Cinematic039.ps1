param(
    [Parameter(Mandatory=$true)][string]$OfficialArchive,
    [Parameter(Mandatory=$true)][string]$NativeDirectory,
    [Parameter(Mandatory=$true)][string]$LauncherDirectory,
    [Parameter(Mandatory=$true)][string]$WorkDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$officialHash = 'e208370d0f7db38c91f5b9169cca8c57eee8a1f3f7621ac9756a0c9f65d83c85'
if ((Get-FileHash -LiteralPath $OfficialArchive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $officialHash) { throw 'The official archive is not the pinned v0.9.39-beta.29 release' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputRoot) { throw 'Output folder already exists; choose a new empty destination' }
$stageRoot = Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ('package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
Expand-Archive -LiteralPath $OfficialArchive -DestinationPath (Join-Path $stageRoot 'official')
$starters = @(Get-ChildItem -LiteralPath (Join-Path $stageRoot 'official') -Recurse -File -Filter bb-start.exe | Where-Object { $_.FullName -match '[\\/]engine[\\/]out[\\/]bb-start\.exe$' })
if ($starters.Count -ne 1) { throw 'Unexpected official package layout' }
$packageRoot = $starters[0].Directory.Parent.Parent.FullName
$nativeOut = Join-Path ([IO.Path]::GetFullPath($NativeDirectory)) 'engine/out'
foreach ($name in @('bb-probe.exe','bb-engine-assets.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $nativeOut $name))) { throw "Missing patched native executable: $name" }
}
if (-not (Test-Path -LiteralPath (Join-Path $LauncherDirectory 'Launcher.exe'))) { throw 'Missing cinematic Launcher.exe' }
$nativeManifest = Join-Path $NativeDirectory 'native-build.json'
if (-not (Test-Path -LiteralPath $nativeManifest)) { throw 'Missing native build provenance' }
$nativeBuild = Get-Content -LiteralPath $nativeManifest -Raw | ConvertFrom-Json
if ($nativeBuild.tag -ne 'v0.9.39-beta.29' -or $nativeBuild.upstreamCommit -ne 'e6b943dd969d99561545315f3d801f10e4a6cf8d') { throw 'Native build targets a different release' }
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
Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'Launcher.exe') -Destination (Join-Path $outputRoot 'Launcher.exe')
$manifest = [ordered]@{
    tag = 'v0.9.39-beta.29'
    upstreamCommit = 'e6b943dd969d99561545315f3d801f10e4a6cf8d'
    officialArchiveSha256 = $officialHash
    nativeBuild = $nativeBuild
    nativeOutputResolutionHidden = $true
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputRoot 'cinematic-engine.json') -Encoding utf8NoBOM
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UPDATE-039.md') -Destination (Join-Path $outputRoot 'LEEME-ACTUALIZACION.md')
$lines = foreach ($file in Get-ChildItem -LiteralPath $outputRoot -Recurse -File | Sort-Object FullName) {
    ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) + '  ' + [IO.Path]::GetRelativePath($outputRoot, $file.FullName).Replace('\','/')
}
$lines | Set-Content -LiteralPath (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding utf8NoBOM
Write-Output "Verified package: $outputRoot"
