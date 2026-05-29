param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

if ($Version) { $env:VERSION = $Version }

$resolvedVersion = Resolve-BuildVersion
$commit = Get-GitCommit
$commitShort = Get-GitShortSha
$installerVersion = Get-InstallerVersion

Ensure-Directory $script:ArtifactsPackages

$linuxAssets = @()
if (Test-Path $script:ArtifactsLinuxPackages) {
    $tarballPattern = '^directorysync_(?<version>[^_]+)_(?<rid>linux-(?:x64|arm64))\.tar\.gz$'
    Get-ChildItem -Path $script:ArtifactsLinuxPackages -Filter 'directorysync_*.tar.gz' |
        Sort-Object Name |
        ForEach-Object {
            if ($_.Name -notmatch $tarballPattern) { return }
            $linuxAssets += [ordered]@{
                rid        = $Matches.rid
                version    = $Matches.version
                fileName   = $_.Name
                sha256     = Get-FileSha256Hex -Path $_.FullName
                sizeBytes  = $_.Length
            }
        }
}

$windowsAsset = $null
if (Test-Path $script:ArtifactsWindowsPackages) {
    $msi = Get-ChildItem -Path $script:ArtifactsWindowsPackages -Filter 'DirectorySync-*.msi' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($msi) {
        $windowsAsset = [ordered]@{
            fileName  = $msi.Name
            sha256    = Get-FileSha256Hex -Path $msi.FullName
            sizeBytes = $msi.Length
        }
    }
}

$dockerMeta = $null
$dockerReleaseDir = Join-Path $script:RepoRoot 'deploy/docker/release'
$metadataCandidates = @(
    (Join-Path $script:ArtifactsDocker 'image-metadata.json')
    (Join-Path $dockerReleaseDir 'image-metadata.json')
)
foreach ($metadataPath in $metadataCandidates) {
    if (-not (Test-Path $metadataPath)) { continue }
    $dockerMeta = Get-Content $metadataPath -Raw | ConvertFrom-Json
    break
}

if (-not $dockerMeta) {
    $tagsCandidates = @(
        (Join-Path $script:ArtifactsDocker 'image-tags.txt')
        (Join-Path $dockerReleaseDir 'image-tags.txt')
    )
    foreach ($tagsPath in $tagsCandidates) {
        if (-not (Test-Path $tagsPath)) { continue }
        $tags = Get-Content $tagsPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        if (-not $tags) { continue }
        $primary = $tags[0]
        $image = if ($primary -match '^(?<image>[^:]+)(?::|$)') { $Matches.image } else { $primary }
        $dockerMeta = [ordered]@{
            image      = $image
            primaryTag = $primary
            tags       = @($tags)
            digest     = $null
        }
        break
    }
}

$manifest = [ordered]@{
    schemaVersion    = 1
    version          = $resolvedVersion
    gitCommit        = $commit
    gitCommitShort   = $commitShort
    installerVersion = $installerVersion
    linux            = @($linuxAssets)
    windows          = $windowsAsset
    docker           = $dockerMeta
}

$json = $manifest | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($script:ReleaseManifest, "${json}`n", [System.Text.UTF8Encoding]::new($false))
Write-BuildLog "generate-release-manifest wrote=$($script:ReleaseManifest)"

$linuxManifest = Join-Path $script:ArtifactsLinuxPackages 'release-manifest.json'
if (Test-Path $script:ArtifactsLinuxPackages) {
    Copy-Item -Path $script:ReleaseManifest -Destination $linuxManifest -Force
    Write-BuildLog "generate-release-manifest staged=$linuxManifest"
}
