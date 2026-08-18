# Quake II RT Classic

Quake II RT Classic is a renderer-focused fork of Quake II RTX. Its default
mode uses the original Quake II textures, MD2 models, animation, geometry,
HUD, menus, and gameplay. Path tracing modernizes light transport without
replacing the 1997 artwork.

## What the classic path does

- Stops the automatic `overrides/` texture lookup and loads the requested
  vanilla WAL/PCX image instead.
- Keeps material definitions only for semantic information such as water,
  glass, lava, screens, light flags, and radiance.
- Discards replacement base, normal, emissive, and mask maps.
- Prefers the requested original MD2 model instead of an MD3 replacement.
- Uses a matte fallback for ordinary surfaces: roughness `0.9`, metallic `0`,
  specular factor `0.05`, and no normal map.
- Synthesizes emissive masks from original light textures where material or
  BSP light flags require them.
- Scales indirect lighting and indirect specular response conservatively.
- Disables Q2RTX's added flare-gun loadout so the original weapon roster and
  gameplay remain the default.

## Settings

The recommended preset is loaded from `baseq2/rt_classic.cfg`.

| Setting | Default | Purpose |
| --- | ---: | --- |
| `rt_classic` | `1` | Enables RT Classic renderer tuning. |
| `rt_original_textures` | `1` | Bypasses automatic and material texture replacements. |
| `rt_original_models` | `1` | Prefers requested MD2 models over MD3 replacements. |
| `rt_classic_materials` | `1` | Applies the conservative fallback to ordinary surfaces. |
| `rt_bounce_strength` | `0.5` | Scales indirect light without brightening every room. |
| `rt_reflections` | `1` | Enables reflection/refraction rays for special surfaces. |
| `rt_reflection_strength` | `0.25` | Scales rough indirect specular response. |
| `rt_emissive_strength` | `1.0` | Scales emissive appearance and BSP surface lights. |

The complete preset enables the physical atmosphere so its moving ray-traced
sun, dynamic sky illumination, volumetric shafts, and time-of-day changes are
clearly visible. World, model, and HUD artwork still follows the original-art
routing. Set `physical_sky 0` manually if a static original skybox is preferred;
direct sun remains available in Classic mode.

Changes that affect loaded images or materials take effect after a map reload
or renderer restart. Setting `rt_classic 0`, `rt_original_textures 0`, and
`rt_original_models 0` restores the upstream replacement paths.

## Original game data

The source tree does not redistribute Quake II's commercial data. Copy or
link `pak0.pak`, `pak1.pak`, and `pak2.pak` from a legally installed copy of
Quake II into the runtime `baseq2` directory. Do not extract or modify them.

On Windows, the included helper creates same-volume hard links without
duplicating or altering the original files:

```powershell
.\scripts\link_quake2_data.ps1 -Quake2Path 'C:\path\to\Quake 2'
```

The official upstream `blue_noise.pkz` and `q2rtx_media.pkz` packages are
required for the complete ray-traced presentation. `q2rtx_media.pkz` contains
renderer support data as well as replacement art; RT Classic loads the support
data while its original-art routing rejects replacement base-color, normal,
emissive, and model assets. If `blue_noise.pkz` is absent, RT Classic can still
start with deterministic procedural sampling noise, but that fallback is for
diagnostics and is not a substitute for a complete distribution.
