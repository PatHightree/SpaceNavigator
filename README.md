SpaceNavigator
==============

SpaceNavigator driver for Unity3D

This driver lets you fly around your scene and allows you to move stuff around.  
You can also use it at runtime via scripting.  

The default mode is **Fly** mode and when you're flying around, this driver always keeps your horizon horizontal.  
So you don't have to worry about ending up upside down, just go where you want and get some work done.  
To move stuff around, you can use 2 modes: Telekinesis and GrabMove.  
In **Telekinesis** mode, you can move the stuff you selected with the SpaceNavigator, while your camera stays put.  
(this mode can be operated in Camera-, World-, Parent- and Local coordinates)  
In **GrabMove** mode the stuff will be linked to your camera so you can take it with you and position it where you want.  
Translation can be snapped to a grid and rotation can be angle-snapped.  

At the moment it is windows-only, but the code is right [here](https://github.com/PatHightree/SpaceNavigator). 
Who knows, somebody with a mac might just step in and code the mac implementation. 
I've prepared the code to make this as painless as possible.  

The goods
---------
- [SpaceNavigator.unitypackage](http://www.xs4all.nl/~hightree/Unity/SpaceNavigator/SpaceNavigator.unitypackage)
- [SpaceNavigator+DemoScene.unitypackage](http://www.xs4all.nl/~hightree/Unity/SpaceNavigator/SpaceNavigator+demoscene.unitypackage)
- Source code on [Github](https://github.com/PatHightree/SpaceNavigator)

Installation
------------
- Install [3DConnexion driver](http://www.3dconnexion.com/service/drivers.html) and make sure it is running
- Import the unitypackage into your project
- Open the SpaceNavigator window from the pull-down menu Window/SpaceNavigator (or hit Alt-S)
- Fly away

Credits
-------
- Quaternion math by Minahito
http://sunday-lab.blogspot.nl/2008/04/get-pitch-yaw-roll-from-quaternion.html
- Test card texture
http://triangularpixels.net/games/development/2009/07/10/digital-test-cards-for-games/
