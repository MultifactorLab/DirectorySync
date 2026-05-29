param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [switch]$Push,
    [string]$Platforms = 'linux/amd64,linux/arm64',
    [string]$Builder,
    [string]$TagsFile,
    [switch]$NoBinfmt
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

Ensure-Directory $script:ArtifactsDocker

$uniqueTags = [System.Collections.Generic.List[string]]::new()
$seen = @{}

function Add-DockerTag {
    param([string]$Value)
    $normalized = $Value.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) { return }
    if ($seen.ContainsKey($normalized)) { return }
    $seen[$normalized] = $true
    [void]$uniqueTags.Add($normalized)
}

Add-DockerTag $Tag
if ($TagsFile -and (Test-Path $TagsFile)) {
    Get-Content $TagsFile | ForEach-Object { Add-DockerTag $_ }
}

$platformList = @(
    $Platforms.Split(',', [System.StringSplitOptions]::RemoveEmptyEntries) |
    ForEach-Object { $_.Trim() }
)
$isMultiPlatform = $platformList.Count -gt 1
$requiresBinfmt = $platformList -contains 'linux/arm64' -or $isMultiPlatform

if ($isMultiPlatform -and -not $Push) {
    throw 'multi-platform build requires -Push (buildx cannot reliably --load manifest lists)'
}

$buildArgs = @(
    'buildx', 'build',
    '-f', $script:Dockerfile,
    '--target', 'runtime',
    '--platform', $Platforms
)

foreach ($t in $uniqueTags) {
    $buildArgs += @('-t', $t)
}

$buildxMetadata = Join-Path $script:ArtifactsDocker 'buildx-metadata.json'
$buildArgs += @('--metadata-file', $buildxMetadata)

if ($Push) {
    $buildArgs += '--push'
} else {
    $buildArgs += '--load'
}

if ($requiresBinfmt -and -not $NoBinfmt) {
    Write-BuildLog 'docker-build installing binfmt handlers for cross-platform emulation'
    docker run --rm --privileged tonistiigi/binfmt --install all
    if ($LASTEXITCODE -ne 0) {
        throw 'failed to install binfmt handlers; rerun with -NoBinfmt only when using native multi-arch builders'
    }
}

if ($Builder) {
    docker buildx use $Builder
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} elseif (-not (docker buildx inspect 2>$null)) {
    docker buildx create --name directorysync --driver docker-container --use | Out-Null
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

docker buildx inspect --bootstrap | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-BuildLog "docker-build tag=$Tag platforms=$Platforms push=$Push tags=$($uniqueTags.Count)"
& docker @buildArgs $script:RepoRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$digest = $null
if (Test-Path $buildxMetadata) {
    $raw = Get-Content $buildxMetadata -Raw | ConvertFrom-Json
    if ($raw.'containerimage.digest') {
        $digest = $raw.'containerimage.digest'
    }
}

if (-not $digest -and $Push) {
    $inspectJson = docker buildx imagetools inspect $Tag --format '{{json .}}' 2>$null
    if ($inspectJson) {
        $inspect = $inspectJson | ConvertFrom-Json
        if ($inspect.manifest.digest) {
            $digest = $inspect.manifest.digest
        }
        elseif ($inspect.digest) {
            $digest = $inspect.digest
        }
    }
}

$primaryTag = $uniqueTags[0]
$imageName = if ($primaryTag -match '^(?<image>[^:]+)(?::|$)') { $Matches.image } else { $primaryTag }

$metadata = [ordered]@{
    image      = $imageName
    primaryTag = $primaryTag
    tags       = @($uniqueTags)
    digest     = $digest
}

$tagsFilePath = Join-Path $script:ArtifactsDocker 'image-tags.txt'
$metadataPath = Join-Path $script:ArtifactsDocker 'image-metadata.json'
$uniqueTags | Set-Content -Path $tagsFilePath -Encoding UTF8
($metadata | ConvertTo-Json -Depth 6) + "`n" | Set-Content -Path $metadataPath -Encoding UTF8

Write-BuildLog "docker-build wrote=$tagsFilePath"
$digestLabel = if ($digest) { $digest } else { '<none>' }
Write-BuildLog "docker-build wrote=$metadataPath digest=$digestLabel"
Write-BuildLog 'docker-build complete'
