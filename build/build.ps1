param(
    [Parameter(Position = 0)]
    [string]$Command,

    [string]$Rid,
    [string]$Version,
    [string]$Tag,
    [switch]$Push,
    [string]$Platforms = 'linux/amd64,linux/arm64',
    [string]$Builder,
    [string]$TagsFile,
    [switch]$NoBinfmt,
    [switch]$LocalNuget,
    [switch]$NoStageReleaseAssets
)

$ErrorActionPreference = 'Stop'
$BuildDir = $PSScriptRoot
$ScriptsDir = Join-Path $BuildDir 'scripts'

function Show-BuildUsage {
    @'
Usage: build/build.ps1 -Command <name> [options]

Commands:
  restore
  build
  test
  publish-linux   -Rid <rid> [-Version <version>] [-LocalNuget]
  package-linux   -Rid <rid> [-Version <version>] [-NoStageReleaseAssets] [-LocalNuget]
  stage-linux-release
  generate-release-manifest [-Version <version>]
  stage-offline-release     [-Version <version>]
  package-msi     [-Version <version>] [-LocalNuget]
  docker-build    -Tag <tag> [-Push] [-Platforms <list>] [-Builder <name>] [-TagsFile <path>] [-NoBinfmt] [-LocalNuget]
  clean
  all             [-Version <version>] [-LocalNuget]

Environment:
  USE_LOCAL_NUGET  1 = nuget.config.local; 0/empty = nuget.config
'@ | Write-Host
}

function Invoke-BuildScript {
    param(
        [string]$Name,
        [hashtable]$Params = @{}
    )
    $scriptPath = Join-Path $ScriptsDir $Name
    if (-not (Test-Path $scriptPath)) {
        throw "script not found: $scriptPath"
    }
    & $scriptPath @Params
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not $Command) {
    Show-BuildUsage
    exit 1
}

if ($Command -in @('help', '-h', '--help')) {
    Show-BuildUsage
    exit 0
}

if ($LocalNuget) {
    $env:USE_LOCAL_NUGET = '1'
}

. (Join-Path $ScriptsDir 'common.ps1')

switch ($Command) {
    'restore' {
        Write-BuildLog "restore solution=$($script:Solution)"
        dotnet restore $script:Solution --configfile $script:NugetConfig
    }
    'build' {
        $versionProps = Get-DotnetVersionProperties
        Write-BuildLog "build configuration=$($script:BuildConfiguration) version=$(Resolve-BuildVersion)"
        dotnet build $script:Solution -c $script:BuildConfiguration --no-restore $script:DotnetWarnAsMessage @versionProps
    }
    'test' {
        Write-BuildLog "test configuration=$($script:BuildConfiguration)"
        dotnet test $script:Solution -c $script:BuildConfiguration --no-build --verbosity normal $script:DotnetWarnAsMessage
    }
    'publish-linux' {
        if (-not $Rid) { throw 'publish-linux requires -Rid' }
        Invoke-BuildScript 'publish-linux.ps1' @{ Rid = $Rid; Version = $Version }
    }
    'package-linux' {
        if (-not $Rid) { throw 'package-linux requires -Rid' }
        $params = @{ Rid = $Rid; Version = $Version }
        if ($NoStageReleaseAssets) { $params.NoStageReleaseAssets = $true }
        Invoke-BuildScript 'package-linux.ps1' $params
    }
    'package-msi' {
        Invoke-BuildScript 'package-windows-msi.ps1' @{ Version = $Version }
    }
    'docker-build' {
        if (-not $Tag) { throw 'docker-build requires -Tag' }
        $dockerParams = @{
            Tag = $Tag
            Push = $Push
            Platforms = $Platforms
            Builder = $Builder
        }
        if ($TagsFile) { $dockerParams.TagsFile = $TagsFile }
        if ($NoBinfmt) { $dockerParams.NoBinfmt = $true }
        Invoke-BuildScript 'docker-build.ps1' $dockerParams
    }
    'stage-linux-release' {
        Copy-LinuxReleaseAssets -Destination $script:ArtifactsLinuxPackages
        $tarballs = Get-ChildItem -Path $script:ArtifactsLinuxPackages -Filter 'directorysync_*.tar.gz'
        if (-not $tarballs) { throw "no linux tarballs found in $($script:ArtifactsLinuxPackages)" }
        $lines = foreach ($item in $tarballs) {
            $hash = (Get-FileHash -Path $item.FullName -Algorithm SHA256).Hash.ToLower()
            "$hash  $($item.Name)"
        }
        $lines | Set-Content -Path (Join-Path $script:ArtifactsLinuxPackages 'checksums.txt') -Encoding UTF8
        Invoke-BuildScript 'generate-release-manifest.ps1' @{ Version = $Version }
        Write-BuildLog "stage-linux-release complete dir=$($script:ArtifactsLinuxPackages)"
    }
    'generate-release-manifest' {
        Invoke-BuildScript 'generate-release-manifest.ps1' @{ Version = $Version }
    }
    'stage-offline-release' {
        Invoke-BuildScript 'stage-offline-release.ps1' @{ Version = $Version }
    }
    'clean' {
        Write-BuildLog "clean artifacts=$($script:ArtifactsRoot)"
        if (Test-Path $script:ArtifactsRoot) {
            Remove-Item -Recurse -Force $script:ArtifactsRoot
        }
    }
    'all' {
        if ($Version) { $env:VERSION = $Version }
        $commonArgs = @{}
        if ($LocalNuget) { $commonArgs.LocalNuget = $true }
        & $PSCommandPath -Command restore @commonArgs
        & $PSCommandPath -Command build @commonArgs
        & $PSCommandPath -Command test @commonArgs
        & $PSCommandPath -Command publish-linux -Rid linux-x64 -Version $Version @commonArgs
        & $PSCommandPath -Command package-linux -Rid linux-x64 -Version $Version -NoStageReleaseAssets @commonArgs
        & $PSCommandPath -Command publish-linux -Rid linux-arm64 -Version $Version @commonArgs
        & $PSCommandPath -Command package-linux -Rid linux-arm64 -Version $Version @commonArgs
    }
    default {
        throw "unknown command: $Command"
    }
}
