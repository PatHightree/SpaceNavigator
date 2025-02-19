#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        class CoordinateSystem : EditorToolbarDropdown
        {
            public const string ID = "SpaceNavigator/CoordinateSystem";

            public CoordinateSystem()
            {
                switch (Settings.CoordSys)
                {
                    case SpaceNavigatorDriver.CoordinateSystem.Camera: text = "Camera"; break;
                    case SpaceNavigatorDriver.CoordinateSystem.World: text = "World"; break;
                    case SpaceNavigatorDriver.CoordinateSystem.Parent: text = "Parent"; break;
                    case SpaceNavigatorDriver.CoordinateSystem.Local: text = "Local"; break;
                    default: throw new ArgumentOutOfRangeException();
                }
                clicked += ShowDropdown;
            }

            void ShowDropdown()
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Camera"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.Camera, () =>
                {
                    text = "Camera";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.Camera;
                    RefreshLayout.Invoke(this, EventArgs.Empty);
                });
                menu.AddItem(new GUIContent("World"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.World, () =>
                {
                    text = "World";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.World;
                    RefreshLayout.Invoke(this, EventArgs.Empty);
                });
                menu.AddItem(new GUIContent("Parent"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.Parent, () =>
                {
                    text = "Parent";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.Parent;
                    RefreshLayout.Invoke(this, EventArgs.Empty);
                });
                menu.AddItem(new GUIContent("Local"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.Local, () =>
                {
                    text = "Local";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.Local;
                    RefreshLayout.Invoke(this, EventArgs.Empty);
                });
                menu.ShowAsContext();
            }
        }
    }
}
#endif