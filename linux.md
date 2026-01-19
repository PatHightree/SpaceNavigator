# SpaceNavigator Linux Support

Unity's Input System does not support HID devices on Linux, so we cannot use it. Thankfully, there already is
a good open-source driver for 3DConnexion 6DOF devices called [spacenavd](https://github.com/FreeSpacenav/spacenavd), 
so we can rely on it instead.

## Sandboxing

If you've installed Unity from snap or flatpak, then SpaceNavigator will not work.
Install Unity via [Unity Hub](https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux) instead.
See [this guide](https://www.reddit.com/r/Fedora/comments/wupxy7/comment/md4yrch/) for RPM-based distros.

If you insist on using a sandboxed environment, please configure it so that the file `/var/run/spnav.sock`
is exposed to the sandbox.

## Installation

### 1. Install [spacenavd](https://github.com/FreeSpacenav/spacenavd) and [libspnav](https://github.com/FreeSpacenav/libspnav).

For Ubuntu and other Debian-like systems, you can just `sudo apt install spacenavd libspnav0`.
For Fedora, it's `sudo dnf install spacenavd libspnav`.

Before you continue, make sure the driver works by trying it out in another application like Blender.

### 2. Make spacenavd listen on the correct socket

Create a file under `/etc/spnavrc` with the following contents:

    socket=/var/run/spnav.sock

### 3. Set up the spacenavd service

Enable the service with `sudo systemctl enable spacenavd`  
Then start the spacenavd service with `sudo systemctl start spacenavd`, the blue light on the device should now come on.

If you are successful, the output of `file /var/run/spnav.sock` will look like this:

    /var/run/spnav.sock: socket

### 4. Install the SpaceNavigator Driver package in Unity

... as per instructions in the main [README](./README.md) file. Your 6DOF device should now work in the editor.
If it doesn't, try unchecking the "Only navigate when Unity has focus" option.

