TUINRTX

TuinRTX is a custom recompilation of NVIDIA Quake II RTX 1.8.1, rebuilt and
modified for a deliberately classic presentation: the original 1997 Quake II
textures, models, HUD, gameplay, and atmosphere are paired with real-time
path-traced lighting, dynamic sunlight, bounced light, shadows, reflections,
and a ray-traced flashlight.

This is not merely a configuration file or texture pack. The engine and its
shaders were recompiled with custom Classic-mode rendering changes.

FIRST START
-----------

TuinRTX does not redistribute the commercial Quake II game data. The launcher
will scan your Steam libraries and common installation folders for a legally
installed copy of Quake II. It needs these files:

  baseq2\pak0.pak
  baseq2\pak1.pak (when available)
  baseq2\pak2.pak (when available)

If automatic detection fails, select your Quake II installation folder in the
launcher. The launcher links or copies the files into TuinRTX without modifying
the originals. Choose the folder that contains baseq2\pak0.pak. A typical Steam
installation is:

  C:\Program Files (x86)\Steam\steamapps\common\Quake 2

After scanning, the launcher clearly reports QUAKE II FOUND or QUAKE II NOT
FOUND. First-start loading time can vary while RTX resources and shaders are
initialized.

The default TuinRTX install location is:

  %LOCALAPPDATA%\Programs\TuinRTX

The installer lets you select an exact different folder before installation.
It can create a desktop shortcut that always opens TuinRTX Launcher.

UNINSTALL
---------

Use any of these options:

  * Click "Uninstall TuinRTX" inside TuinRTX Launcher.
  * Open the Start Menu and choose TuinRTX > Uninstall TuinRTX.
  * Open Windows Settings > Apps > Installed apps and uninstall TuinRTX.

Uninstalling TuinRTX removes its own files and shortcuts only. The original
Quake II installation and its commercial PAK files remain untouched.

QUICK HELP
----------

  /        Cycle the time of day / scenery lighting
  F        Toggle the ray-traced flashlight
  ~        Open the console
  WASD     Move
  Mouse 1  Fire
  F5       Video settings
  F12      Screenshot

Ray-tracing rays are not drawn as visible lines. Their results appear as
dynamic shadows, bounced colored light, reflections, sunlight, and light
shafts. Time-of-day differences are clearest outdoors or near large openings.
RT Classic keeps texture filtering on nearest-neighbor for the original crisp,
pixelated texture appearance.

CREDITS
-------

  TuinRTX project
    Original concept, creative direction, testing, custom engine changes, and
    promotional screenshots.

  NVIDIA and the Quake II RTX contributors
    Quake II RTX renderer and project foundation.

  Christoph Schied and Q2VKPT contributors
    Original path-traced Quake II research and implementation.

  Q2PRO contributors
    Engine foundation used by Quake II RTX.

  id Software
    Quake II and the original game source.

Quake II game data remains copyrighted by its respective owners and is not
included. TuinRTX source modifications are distributed under GPL-2.0 in
accordance with the upstream project. See license.txt and notice.txt.

Project base: NVIDIA Quake II RTX 1.8.1
TuinRTX build: custom RT Classic recompilation
