SpaceNavigator driver for Unity3D

This driver lets you fly around your scene and allows you to move stuff around.  
You can also use it at runtime via scripting.  

New:
- Mac support.
- The driver works with the SpaceNavigator window closed.

The default mode is Fly mode and when you're flying around, this driver keeps your horizon horizontal.  
So you don't have to worry about ending up upside down, just go where you want and get some work done.  
To comfortably navigate large areas and minute details, you can easilly switch between 3 customizable sensitivity presets.  
To move stuff around, you can use 2 modes: Telekinesis and GrabMove.  
In Telekinesis mode, you can move the stuff you selected with the SpaceNavigator, while your camera stays put.  
(this mode can be operated in Camera-, World-, Parent- and Local coordinates)  
In GrabMove mode the stuff will be linked to your camera so you can take it with you and position it where you want.  
Translation can be snapped to a grid and rotation can be angle-snapped.  

If you have feedback, please use this thread on the Unity forums http://forum.unity3d.com/threads/182382-SpaceNavigator-driver-OpenSource

Platform support
---------
As of version 1.3, this driver supports both PC and Mac.  
The driver supports both Unity Personal and Pro versions.  
Caveat: when running Unity 4 and lower on Mac you need the Pro version because Free did not allow plugins.

Known bugs and limitations
---------
- Grab Mode only works in the camera coordinate system (sorry, I couldn't get my head around the quaternion math of manipulating in one coordinate system while constraining in another)

The goods
---------
- The full package is available on the Unity asset store http://u3d.as/51X
- The package also contains a couple of runtime samples:
  - Fly around.unity: Fly around with a sphere while knocking over some cubes.
  - Folow curve.unity: Make your torus follow the curve, but don't touch it!
- The source code is available on Github https://github.com/PatHightree/SpaceNavigator

Installation
---------
- Install 3DConnexion driver and make sure it is running http://www.3dconnexion.com/service/drivers.html
- Import the unitypackage into your project
- Open the SpaceNavigator window from the pull-down menu Window/SpaceNavigator (or hit Alt-S)
- Fly away

Upgrading
---------
When installing a newer version of the plugin, please follow these steps:
- Close the SpaceNavigator editor window.
- Delete the SpaceNavigator folder from your project.  
- Delete Plugins\3DConnexionWrapperU4.bundle from your project.
- Import the new package.
- Reopen the SpaceNavigator window (Alt-S).  

If you delete the folder while the SpaceNavigator window is still open, Unity will throw some errors.
When this happens, choose the default layout from the layout dropdown in the top right of Unity's UI and everything should return to normal.

Pro tip
-------
Copy the SpaceNavigator.unitypackage to Unity/Editor/Standard Packages directory.  
- SpaceNavigator is added to the packages list in the project creation in wizard.  
- Easy to add later by right-clicking in Project View and choosing Import Package.  

Credits
-------
- Thanks to Stephen Wolter for further refinement to the mac drift fix. 
- Thanks to Enrico Tuttobene for contributing the mac drift fix.
- Big thanks to Chase Cobb for motivating me to implement the mac version.
- Thanks to Manuela Maier and Dave Buchhoffer (@vsaitoo) for testing and development feedback.
- Thanks to Ewoud Wijma for loaning me the Hackingtosh for building the Mac port.
- Quaternion math by Minahito
  http://sunday-lab.blogspot.nl/2008/04/get-pitch-yaw-roll-from-quaternion.html