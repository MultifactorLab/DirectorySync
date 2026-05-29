param(
    [Parameter(Mandatory = $true)]
    [string]$Rid,

    [string]$Version
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

if ($Version) { $env:VERSION = $Version }

$resolvedVersion = Resolve-BuildVersion
$out = Get-PublishOutputDir -Rid $Rid
Ensure-Directory $out

$versionProps = Get-DotnetVersionProperties

Write-BuildLog "publish-linux rid=$Rid version=$resolvedVersion output=$out"

dotnet publish $script:ConsoleProject `
    -c $script:BuildConfiguration `
    -r $Rid `
    --self-contained true `
    -o $out `
    --configfile $script:NugetConfig `
    @versionProps `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=None `
    /p:DebugSymbols=false

Write-BuildLog "publish-linux complete: $out"
