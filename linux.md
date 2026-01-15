# SpaceNavigator Linux Support

Unity's Input System does not support HID devices on Linux, so we cannot use it. Thankfully, there already is
a good open-source driver for 3DConnexion 6DOF devices called [spacenavd](https://github.com/FreeSpacenav/spacenavd), 
so we can rely on it instead.

## Installation

### 1. Install [spacenavd](https://github.com/FreeSpacenav/spacenavd).

For Ubuntu and other Debian-like systems, you can just `apt install spacenavd`.

Before you continue, make sure the driver works by trying it out in another application like Blender.

### 2. Make spacenavd listen on the correct socket

Create a file under `/etc/spnavrc` with the following contents:

    socket=/var/run/spnav.sock

Then restart the spacenavd service.

If you are successful, the output of `file /var/run/spnav.sock` will look like this:

    /var/run/spnav.sock: socket

### 3. Install SpaceNavigator normally

... as per instructions in the main [README](./README.md) file. Your 6DOF device should now work in the editor.
If it doesn't, try unchecking the "Only navigate when Unity has focus" option.
