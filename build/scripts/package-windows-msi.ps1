param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

if ($Version) { $env:VERSION = $Version }
$resolvedVersion = Resolve-BuildVersion
Ensure-Directory $script:ArtifactsWindowsPackages

$versionProps = Get-DotnetVersionProperties

Write-BuildLog "package-msi version=$resolvedVersion"

dotnet build $script:WixProject `
    -c $script:BuildConfiguration `
    -p:Platform=x64 `
    -p:Configuration=$script:BuildConfiguration `
    --configfile $script:NugetConfig `
    $script:DotnetWarnAsMessage `
    @versionProps

$msi = Get-ChildItem -Path (Join-Path $script:RepoRoot 'deploy/windows/DirectorySync.Installer/bin/x64') `
    -Filter '*.msi' -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $msi) {
    throw 'MSI not found under deploy/windows/DirectorySync.Installer/bin/x64'
}

$destName = "DirectorySync-${resolvedVersion}.msi"
$destPath = Join-Path $script:ArtifactsWindowsPackages $destName
Copy-Item -Path $msi.FullName -Destination $destPath -Force
Write-BuildLog "package-msi wrote=$destPath"
