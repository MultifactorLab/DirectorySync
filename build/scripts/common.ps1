# Shared path constants and helpers for build scripts.
$ErrorActionPreference = 'Stop'

$script:BuildDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$script:RepoRoot = (Resolve-Path (Join-Path $script:BuildDir '..')).Path

$script:Solution = Join-Path $script:RepoRoot 'src/DirectorySync.sln'
$script:ConsoleProject = Join-Path $script:RepoRoot 'src/hosts/DirectorySync.Host.Console/DirectorySync.Host.Console.csproj'
$script:WindowsServiceProject = Join-Path $script:RepoRoot 'src/hosts/DirectorySync.Host.WindowsService/DirectorySync.Host.WindowsService.csproj'
$script:NugetConfigDefault = Join-Path $script:RepoRoot 'nuget.config'
$script:NugetConfigLocal = Join-Path $script:RepoRoot 'nuget.config.local'

function Write-BuildLog {
    param([string]$Message)
    Write-Host "[build] $Message"
}

function Resolve-BuildNugetConfig {
    $default = $script:NugetConfigDefault
    if ($env:USE_LOCAL_NUGET -eq '1') {
        if (-not (Test-Path $script:NugetConfigLocal)) {
            throw "USE_LOCAL_NUGET=1 requires $($script:NugetConfigLocal)"
        }
        Write-BuildLog "nuget config=$($script:NugetConfigLocal) (USE_LOCAL_NUGET=1)"
        return $script:NugetConfigLocal
    }
    return $default
}

$script:NugetConfig = Resolve-BuildNugetConfig
$script:LinuxDeploy = Join-Path $script:RepoRoot 'deploy/linux'
$script:Dockerfile = Join-Path $script:RepoRoot 'deploy/docker/Dockerfile'
$script:WixProject = Join-Path $script:RepoRoot 'deploy/windows/DirectorySync.Installer/DirectorySync.Installer.wixproj'

$script:ArtifactsRoot = Join-Path $script:RepoRoot 'artifacts'
$script:ArtifactsPublish = Join-Path $script:ArtifactsRoot 'publish'
$script:ArtifactsPackages = Join-Path $script:ArtifactsRoot 'packages'
$script:ArtifactsLinuxPackages = Join-Path $script:ArtifactsPackages 'linux'
$script:ArtifactsWindowsPackages = Join-Path $script:ArtifactsPackages 'windows'
$script:ArtifactsDocker = Join-Path $script:ArtifactsRoot 'docker'
$script:ReleaseManifest = Join-Path $script:ArtifactsPackages 'release-manifest.json'
$script:ReleasesRoot = Join-Path $script:RepoRoot 'releases'

$script:BuildConfiguration = if ($env:BUILD_CONFIGURATION) { $env:BUILD_CONFIGURATION } else { 'Release' }
$script:DotnetWarnAsMessage = if ($env:DOTNET_WARN_AS_MESSAGE) { $env:DOTNET_WARN_AS_MESSAGE } else { '-warnasmessage:*' }

function Resolve-BuildVersion {
    if ($env:VERSION) { return $env:VERSION }
    if ($env:CI_COMMIT_TAG) { return $env:CI_COMMIT_TAG.TrimStart('v') }
    if ($env:GITHUB_REF_NAME) { return $env:GITHUB_REF_NAME.TrimStart('v') }
    return '0.0.0-dev'
}

function Get-GitCommit {
    try {
        Push-Location $script:RepoRoot
        $null = git rev-parse --is-inside-work-tree 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        return (git rev-parse HEAD).Trim()
    }
    catch {
        return $null
    }
    finally {
        Pop-Location
    }
}

function Get-InstallerVersion {
    $constants = Join-Path $script:LinuxDeploy 'lib/constants.sh'
    if (-not (Test-Path $constants)) {
        throw "installer constants missing: $constants"
    }
    foreach ($line in Get-Content $constants) {
        if ($line -match '^INSTALLER_VERSION="([^"]+)"') {
            return $Matches[1]
        }
    }
    throw "INSTALLER_VERSION not found in $constants"
}

function Get-FileSha256Hex {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLower()
}

function Get-OfflineReleaseDir {
    param([Parameter(Mandatory = $true)][string]$Version)
    return Join-Path $script:ReleasesRoot "v$Version"
}

function Get-GitShortSha {
    try {
        Push-Location $script:RepoRoot
        $null = git rev-parse --is-inside-work-tree 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        return (git rev-parse --short HEAD).Trim()
    }
    catch {
        return $null
    }
    finally {
        Pop-Location
    }
}

function Get-AssemblyFileVersion {
    param([string]$Version)
    if ($Version -match '^(\d+)\.(\d+)\.(\d+)') {
        return "$($Matches[1]).$($Matches[2]).$($Matches[3]).0"
    }
    if ($Version -match '^(\d+)\.(\d+)') {
        return "$($Matches[1]).$($Matches[2]).0.0"
    }
    return '0.0.0.0'
}

function Get-DotnetVersionProperties {
    $version = Resolve-BuildVersion
    $assemblyVersion = Get-AssemblyFileVersion -Version $version
    $sha = Get-GitShortSha
    $informational = if ($sha) { "${version}+${sha}" } else { $version }
    return @(
        "/p:Version=$version"
        "/p:PackageVersion=$version"
        "/p:AssemblyVersion=$assemblyVersion"
        "/p:FileVersion=$assemblyVersion"
        "/p:InformationalVersion=$informational"
    )
}

function Get-LinuxTarballName {
    param([string]$Version, [string]$Rid)
    return "directorysync_${Version}_${Rid}.tar.gz"
}

function Get-PublishOutputDir {
    param([string]$Rid)
    return Join-Path $script:ArtifactsPublish $Rid
}

function Ensure-Directory {
    param([string]$Path)
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Convert-ToUnixLineEnding {
    param([Parameter(Mandatory = $true)][string]$Path)

    $content = [System.IO.File]::ReadAllText($Path)
    $normalized = $content -replace "`r`n", "`n" -replace "`r", "`n"
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $normalized, $utf8NoBom)
}

function Copy-LinuxReleaseAssets {
    param([Parameter(Mandatory = $true)][string]$Destination)

    Ensure-Directory $Destination

    Copy-Item (Join-Path $script:LinuxDeploy 'install.sh') (Join-Path $Destination 'install.sh') -Force
    Copy-Item (Join-Path $script:LinuxDeploy 'uninstall.sh') (Join-Path $Destination 'uninstall.sh') -Force

    $libDest = Join-Path $Destination 'lib'
    if (Test-Path $libDest) { Remove-Item -Recurse -Force $libDest }
    Copy-Item (Join-Path $script:LinuxDeploy 'lib') $libDest -Recurse -Force

    $scriptPaths = @(
        Join-Path $Destination 'install.sh'
        Join-Path $Destination 'uninstall.sh'
    ) + @(Get-ChildItem -Path $libDest -Recurse -File -Filter '*.sh' | ForEach-Object { $_.FullName })

    foreach ($path in $scriptPaths) {
        Convert-ToUnixLineEnding -Path $path
    }
}
