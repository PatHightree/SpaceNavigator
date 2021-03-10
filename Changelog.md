# Changelog

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