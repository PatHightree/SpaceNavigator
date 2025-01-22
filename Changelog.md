# Changelog

2.0.0-pre.13
- Applied Jonathan Owen's fixes for SpaceMouse Wireless
- Added note how to get runtime editor navigation working
- Fixed timer for auto-saving settings
- Fixed more UnityEditor namespace causing errors in build

2.0.0-pre.12
- Fixed UnityEditor namespace error during player build

2.0.0-pre.11
- Added editor toolbar : switch mode, speed gear, presentation mode and open settings window
- Cleaned up settings window
- Added option to only navigate when Unity has focus, using PR from Jay Ott
- Added option to toggle led when unity gains/loses focus

2.0.0-pre.10
- Merged improvement by AJ Campbell which implements framerate independent navigation
- Added a fix for drift + recalibration button

2.0.0-pre.9
- Merged fix by cpetry to prevent exception when quitting without SpaceNavigator connected

2.0.0-pre.8
- Merged PR by Stefan Beer which fixes drifting (issue #29, #51)

2.0.0-beta.7
- Merged PR by krisrok which enables toggling the SpaceNavigator's LED
- Merged PR by Jiapeng Chi which fixes rotation offsets between editor and play mode

2.0.0-beta.6
- Added separate layout for SpaceNavigator Wireless
- Merged PR by Felix Herbst to match trailing spaces in manufacturer name, fixes Space Pilot Pro and possibly others

2.0.0-beta.5
- Added Input Device Helper by Felix Herbst, can be used to supply HID descriptor data when reporting issues for unsupported 3dconnexion devices

2.0.0-beta.4
- Added debug logs  
  Enable logging by adding SPACENAVIGATOR_DEBUG to Project Settings/Player/Scripting Define Symbols

2.0.0-beta.3
- Sensitivity settings are applied in editor
- Axis locks are applied in editor
- Excluded ViewportController editor-only code from build

2.0.0-beta.2
- Added error message when new input system is not active
- Added option to have both input systems active to readme

2.0.0-beta.1
- Changed git package url in readme to /develop branch for beta release

2.0.0-alpha.2 to 8
- Package manager + git experiments
- Prepare for beta publishing

2.0.0-alpha.1
- Refactored driver to work on the new Unity Input System.
- Restructured project as a Unity custom package.

1.5.2
Fixes
- Fixed sceneview camera pos/rot being reset on play/stop.
- Settings are now saved immediately after any modification.

1.5.1
Fixes
- Merged Stephen Wolter's deadzone fix for drifting on mac.
- Added SpaceNavigatorDriver namespace.
- Renamed a script that was conflicting with another sdk.

1.5
Features
- Added option to navigate in the editor at runtime.
- Separate axis lock settings for navigation and manipulation modes.
- Autosave settings every 30 seconds.  
Fixes
- Fixed horizon lock, coord sys and snap setting not being saved.
- Refactored driver settings.

1.4.1
- Fixed drift on Mac (contributed by Enrico Tuttobene)

1.4
- Driver works without having the editor window open.
- Fixed inverted yaw axis on PC.
- Tweaked sensitivity scale of PC and Mac implementations.
- Fixed errors in Follow Curve sample in Unity 5.

1.3.3
- Mac support.
- Added XCode project for Mac 3DConnexionWrapper (see github).
- Fixed incorrect default sensitivity presets.
- Added scroll bars to editor window.
- Lined up checkboxes in editor window.