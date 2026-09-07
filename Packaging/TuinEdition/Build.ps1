param(
 [Parameter(Mandatory=$true)][string]$RuntimePath,
 [Parameter(Mandatory=$true)][string]$LayerPath,
 [Parameter(Mandatory=$true)][string]$ConverterPath,
 [string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../..").Path
$out=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $repo 'build/tuin-release'}
$payload=Join-Path $out ('payload-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force "$payload/game/baseq2","$payload/layer","$payload/Licenses","$payload/tools","$payload/game/reshade-shaders/Shaders" | Out-Null
foreach($name in @('q2rtx.exe','nvngx_dlss.dll','dlss5-feed.addon64')){Copy-Item "$RuntimePath/$name" "$payload/game"}
foreach($name in @('blue_noise.pkz','q2rtx_media.pkz','gamex86_64.dll','q2rtx.cfg','rt_classic.cfg','q2rtx.menu','prefetch.txt','pt_toggles.cfg')){Copy-Item "$RuntimePath/baseq2/$name" "$payload/game/baseq2"}
foreach($name in @('shader_vkpt','materials','maps','pics')){Copy-Item "$RuntimePath/baseq2/$name" "$payload/game/baseq2" -Recurse}
foreach($name in @('ReShade.fxh','ReShadeUI.fxh','DLSS5_Feed.fx')){Copy-Item "$RuntimePath/reshade-shaders/Shaders/$name" "$payload/game/reshade-shaders/Shaders"}
Copy-Item "$LayerPath/ReShade64.dll","$LayerPath/ReShade64.json" "$payload/layer"
Copy-Item "$ConverterPath/*" "$payload/tools" -Recurse
Copy-Item "$PSScriptRoot/licenses/*" "$payload/Licenses"
Copy-Item "$repo/license.txt","$repo/notice.txt" "$payload/Licenses"
Copy-Item "$PSScriptRoot/README.md" "$payload/README.txt"
Copy-Item "$PSScriptRoot/ReShadePreset.ini","$PSScriptRoot/dlss5-feed.cfg" "$payload/game"
$bad=Get-ChildItem $payload -Recurse -File | Where-Object {$_.Name -match '^(pak[0-9]+\.pak|z_remastered_models.*|deep-fried-chicken.*|nvngx_dlssnr\.dll|lumenite_.*|q2config.cfg)$'}
if($bad){throw 'Unwanted owned or optional asset in payload'}
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf=Join-Path $framework 'WPF'
$compiler=Join-Path $framework 'csc.exe'
$common=@('/nologo','/target:winexe','/optimize+','/platform:x64',"/win32icon:$PSScriptRoot\assets\tuin.ico",'/r:System.dll','/r:System.Core.dll','/r:System.Xaml.dll','/r:System.Windows.Forms.dll','/r:System.Drawing.dll',"/r:$framework\System.IO.Compression.dll","/r:$framework\System.IO.Compression.FileSystem.dll","/r:$wpf\PresentationCore.dll","/r:$wpf\PresentationFramework.dll","/r:$wpf\WindowsBase.dll", "/resource:$PSScriptRoot\assets\scene2.png,scene2.png","/resource:$PSScriptRoot\assets\icon.png,icon.png")
& $compiler @common "/out:$payload\Tuin Quake II RTX.exe" "$PSScriptRoot\Tuin.cs"
if($LASTEXITCODE){throw 'Launcher compile failed'}
$installer=Join-Path $out 'Tuin-Quake-II-RTX-Setup-v1.1.0.exe'
& $compiler @common '/define:INSTALLER' "/out:$installer" "$PSScriptRoot\Tuin.cs"
if($LASTEXITCODE){throw 'Installer compile failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $out ('payload-'+[Guid]::NewGuid().ToString('N')+'.zip')
[IO.Compression.ZipFile]::CreateFromDirectory($payload,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$stream=[IO.File]::Open($installer,[IO.FileMode]::Append)
try{$offset=$stream.Position;$input=[IO.File]::OpenRead($zip);try{$input.CopyTo($stream)}finally{$input.Dispose()};$bytes=[BitConverter]::GetBytes([long]$offset);$stream.Write($bytes,0,8);$bytes=[Text.Encoding]::ASCII.GetBytes('TUINQ2Z1');$stream.Write($bytes,0,8)}finally{$stream.Dispose()}
Set-Content "$out/launcher-path.txt" "$payload/Tuin Quake II RTX.exe"
Set-Content "$out/payload-path.txt" $payload
Get-Item $installer | Select-Object FullName,Length
