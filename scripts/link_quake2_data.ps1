param(
    [Parameter(Mandatory = $true)]
    [string]$Quake2Path
)

$ErrorActionPreference = 'Stop'
$sourceBase = Join-Path $Quake2Path 'baseq2'
$targetBase = Join-Path (Split-Path -Parent $PSScriptRoot) 'baseq2'

if (-not (Test-Path -LiteralPath (Join-Path $sourceBase 'pak0.pak'))) {
    throw "No vanilla baseq2/pak0.pak found below '$Quake2Path'."
}

foreach ($pakName in 'pak0.pak', 'pak1.pak', 'pak2.pak') {
    $source = Join-Path $sourceBase $pakName
    if (-not (Test-Path -LiteralPath $source)) {
        continue
    }

    $target = Join-Path $targetBase $pakName
    if (Test-Path -LiteralPath $target) {
        $sourceLength = (Get-Item -LiteralPath $source).Length
        $targetLength = (Get-Item -LiteralPath $target).Length
        if ($sourceLength -ne $targetLength) {
            throw "Refusing to replace existing '$target'; its size differs from '$source'."
        }
        Write-Host "Already present: $target"
        continue
    }

    New-Item -ItemType HardLink -Path $target -Target $source | Out-Null
    Write-Host "Linked: $target -> $source"
}

Write-Host 'Vanilla Quake II data is ready. The original files were not modified.'
