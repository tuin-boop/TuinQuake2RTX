param([Parameter(Mandatory=$true)][string]$LauncherPath)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$fixture=Join-Path $root ('build/test-launcher-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force "$fixture/game/baseq2","$fixture/layer","$fixture/nr","$fixture/game/reshade-shaders/Shaders" | Out-Null
Copy-Item $LauncherPath "$fixture/Tuin Quake II RTX.exe"
Set-Content "$fixture/game/reshade-shaders/Shaders/lumenite_Kernel.fx" 'INERT TEST FIXTURE'
foreach($name in @('deep-fried-chicken.addon64','deep-fried-chicken-nvngx.dll','nvngx_dlssnr.dll')){Set-Content "$fixture/nr/$name" 'INERT TEST FIXTURE'}
Set-Content "$fixture/Capture.cs" @'
using System;using System.IO;
class Capture{static void Main(string[] args){File.WriteAllLines("captured.txt",new[]{string.Join(" ",args),Environment.GetEnvironmentVariable("VK_INSTANCE_LAYERS")??"off",Environment.GetEnvironmentVariable("DISABLE_VK_LAYER_reshade_1")??"missing"});}}
'@
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe "/out:$fixture\game\q2rtx.exe" "$fixture\Capture.cs"
if($LASTEXITCODE){throw 'Fixture compile failed'}
$assembly=[Reflection.Assembly]::LoadFile("$fixture\Tuin Quake II RTX.exe")
$method=$assembly.GetType('Tuin').GetMethod('ImportNR',[Reflection.BindingFlags]'Static,NonPublic')
$method.Invoke($null,@("$fixture/nr","$fixture/game")) | Out-Null
foreach($models in @($false,$true)){
if($models){Set-Content "$fixture/game/baseq2/z_remastered_models.pkz" 'INERT MODEL FIXTURE'}
foreach($mode in @('off','on')){
$p=Start-Process "$fixture/Tuin Quake II RTX.exe" -ArgumentList @('--launch-test','0',$mode) -Wait -PassThru
if($p.ExitCode){throw 'Test launch failed'}
$c=Get-Content "$fixture/game/captured.txt"
if($c[0] -notmatch '\+exec tuin-launch.cfg' -or $c[0] -match '\+map'){throw 'Wrong arguments'}
if($mode -eq 'on' -and $c[1] -ne 'VK_LAYER_quake_swapper'){throw 'Missing optional layer'}
if($mode -eq 'off' -and $c[1] -ne 'off'){throw 'Optional layer leaked'}
if($c[2] -ne '1'){throw 'Global ReShade not excluded'}
if((Get-Content "$fixture/game/baseq2/tuin-launch.cfg" -Raw) -notmatch 'pt_nearest 2'){throw 'Nearest sampling missing'}
if($models -and !(Test-Path "$fixture/game/baseq2/z_remastered_models.pkz")){throw 'Models incorrectly disabled'}
"PASS models=$models NR=$mode"
}}
