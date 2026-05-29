param(
    [Parameter(Mandatory = $true)]
    [string]$Rid,

    [string]$Version,
    [switch]$NoStageReleaseAssets
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

if ($Version) { $env:VERSION = $Version }

$resolvedVersion = Resolve-BuildVersion
$publishDir = Get-PublishOutputDir -Rid $Rid
if (-not (Test-Path $publishDir)) {
    throw "publish output missing (run publish-linux first): $publishDir"
}

Ensure-Directory $script:ArtifactsLinuxPackages

$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("directorysync-package-$([Guid]::NewGuid().ToString('N'))")
New-Item -ItemType Directory -Force -Path $staging | Out-Null
try {
    Copy-Item -Path (Join-Path $publishDir '*') -Destination $staging -Recurse -Force

    $tarball = Get-LinuxTarballName -Version $resolvedVersion -Rid $Rid
    $tarballPath = Join-Path $script:ArtifactsLinuxPackages $tarball

    if (Get-Command tar -ErrorAction SilentlyContinue) {
        tar -C $staging -czf $tarballPath .
    } else {
        throw 'tar command is required for package-linux on this platform'
    }

    Write-BuildLog "package-linux wrote=$tarballPath"

    $checksums = Join-Path $script:ArtifactsLinuxPackages 'checksums.txt'
    $hash = (Get-FileHash -Path $tarballPath -Algorithm SHA256).Hash.ToLower()
    $line = "$hash  $tarball"
    $existing = @()
    if (Test-Path $checksums) {
        $existing = Get-Content $checksums | Where-Object { $_ -notmatch "  $([regex]::Escape($tarball))$" }
    }
    (@($existing) + $line) | Set-Content -Path $checksums -Encoding UTF8
    Write-BuildLog "package-linux updated=$checksums"

    if (-not $NoStageReleaseAssets) {
        Copy-LinuxReleaseAssets -Destination $script:ArtifactsLinuxPackages
        Write-BuildLog "package-linux staged release scripts and lib/ in $($script:ArtifactsLinuxPackages)"
    }
}
finally {
    if (Test-Path $staging) {
        Remove-Item -Recurse -Force $staging
    }
}

Write-BuildLog 'package-linux complete'
