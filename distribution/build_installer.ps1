param(
    [string]$DotNet = 'dotnet',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repo 'artifacts'
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path $PSScriptRoot 'build\payload'
$publish = Join-Path $PSScriptRoot 'build\publish'
$payloadZip = Join-Path $PSScriptRoot 'build\TuinRTX-payload.zip'

foreach ($path in $stage, $publish) {
    if (Test-Path -LiteralPath $path) {
        $resolved = [IO.Path]::GetFullPath($path)
        if (-not $resolved.StartsWith([IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'build')), [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean unexpected path: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}
New-Item -ItemType Directory -Path $output -Force | Out-Null

$launcherProject = Join-Path $PSScriptRoot 'TuinRTX.Launcher\TuinRTX.Launcher.csproj'
$installerProject = Join-Path $PSScriptRoot 'TuinRTX.Installer\TuinRTX.Installer.csproj'
$launcherPublish = Join-Path $publish 'launcher'
$installerPublish = Join-Path $publish 'installer'

& $DotNet publish $launcherProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $launcherPublish
if ($LASTEXITCODE -ne 0) { throw 'Launcher publish failed.' }
& $DotNet publish $installerProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $installerPublish
if ($LASTEXITCODE -ne 0) { throw 'Installer publish failed.' }

Copy-Item -LiteralPath (Join-Path $launcherPublish 'TuinRTX Launcher.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'q2rtx.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'license.txt') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'notice.txt') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'changelog.md') -Destination $stage

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'art') -Destination $stage -Recurse
New-Item -ItemType Directory -Path (Join-Path $stage 'doc') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'doc\rt-classic.md') -Destination (Join-Path $stage 'doc')

$stageBase = Join-Path $stage 'baseq2'
New-Item -ItemType Directory -Path $stageBase -Force | Out-Null
foreach ($file in 'gamex86_64.dll','q2rtx.cfg','rt_classic.cfg','q2rtx.menu','prefetch.txt','pt_toggles.cfg','blue_noise.pkz','q2rtx_media.pkz') {
    $source = Join-Path $repo "baseq2\$file"
    if (-not (Test-Path -LiteralPath $source)) { throw "Required runtime file is missing: $source" }
    Copy-Item -LiteralPath $source -Destination $stageBase
}
foreach ($folder in 'shader_vkpt','materials','maps','pics') {
    $source = Join-Path $repo "baseq2\$folder"
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $stageBase -Recurse }
}

if (Test-Path -LiteralPath $payloadZip) { Remove-Item -LiteralPath $payloadZip -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $payloadZip, [IO.Compression.CompressionLevel]::NoCompression, $false)

$stub = Join-Path $installerPublish 'TuinRTX-Setup.exe'
$setupName = 'TuinRTX-Setup-v1.0.5.exe'
$setup = Join-Path $output $setupName
Copy-Item -LiteralPath $stub -Destination $setup -Force
$setupStream = [IO.File]::Open($setup, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $offset = $setupStream.Position
    $payloadStream = [IO.File]::OpenRead($payloadZip)
    try { $payloadStream.CopyTo($setupStream) } finally { $payloadStream.Dispose() }
    $offsetBytes = [BitConverter]::GetBytes([Int64]$offset)
    $magicBytes = [Text.Encoding]::ASCII.GetBytes('TUINRTX_PAYLOAD1')
    $setupStream.Write($offsetBytes, 0, $offsetBytes.Length)
    $setupStream.Write($magicBytes, 0, $magicBytes.Length)
} finally {
    $setupStream.Dispose()
}

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $setup
$hashLine = "$($hash.Hash.ToLowerInvariant())  $setupName"
[IO.File]::WriteAllText((Join-Path $output 'TuinRTX-Setup-v1.0.5.sha256.txt'), $hashLine + [Environment]::NewLine)

Get-Item -LiteralPath $setup | Select-Object FullName, Length, LastWriteTime
Write-Host "SHA256: $($hash.Hash)"
