param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

if ($Version) { $env:VERSION = $Version }

$resolvedVersion = Resolve-BuildVersion
$dest = Get-OfflineReleaseDir -Version $resolvedVersion
$linuxDest = Join-Path $dest 'linux'
$windowsDest = Join-Path $dest 'windows'

if (-not (Test-Path $script:ArtifactsLinuxPackages)) {
    throw "linux packages missing (run package-linux first): $($script:ArtifactsLinuxPackages)"
}

$tarballs = Get-ChildItem -Path $script:ArtifactsLinuxPackages -Filter 'directorysync_*.tar.gz'
if (-not $tarballs) {
    throw "no linux tarballs in $($script:ArtifactsLinuxPackages)"
}

Ensure-Directory $linuxDest
Ensure-Directory $windowsDest

Copy-Item -Path $tarballs.FullName -Destination $linuxDest -Force

foreach ($asset in @('install.sh', 'uninstall.sh', 'checksums.txt', 'release-manifest.json')) {
    $source = Join-Path $script:ArtifactsLinuxPackages $asset
    if (Test-Path $source) {
        Copy-Item -Path $source -Destination (Join-Path $linuxDest $asset) -Force
    }
}

$libSource = Join-Path $script:ArtifactsLinuxPackages 'lib'
$libDest = Join-Path $linuxDest 'lib'
if (Test-Path $libSource) {
    if (Test-Path $libDest) { Remove-Item -Recurse -Force $libDest }
    Copy-Item -Path $libSource -Destination $libDest -Recurse -Force
}

$checksums = Join-Path $script:ArtifactsLinuxPackages 'checksums.txt'
if (Test-Path $checksums) {
    Copy-Item -Path $checksums -Destination (Join-Path $dest 'checksums.txt') -Force
}

if (-not (Test-Path $script:ReleaseManifest)) {
    & (Join-Path $PSScriptRoot 'generate-release-manifest.ps1') -Version $resolvedVersion
}
Copy-Item -Path $script:ReleaseManifest -Destination (Join-Path $dest 'release-manifest.json') -Force

$msiFiles = @(Get-ChildItem -Path $script:ArtifactsWindowsPackages -Filter 'DirectorySync-*.msi' -ErrorAction SilentlyContinue)
if ($msiFiles) {
    Copy-Item -Path $msiFiles.FullName -Destination $windowsDest -Force
}

Write-BuildLog "stage-offline-release complete dir=$dest"
