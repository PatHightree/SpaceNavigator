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

                Settings.ModeChanged += (sender, args) => MayBeVisibe();
                MayBeVisibe();
            }

            private void MayBeVisibe()
            {
                // visible = Settings.Mode == OperationMode.Telekinesis;
                SetEnabled(Settings.Mode == OperationMode.Telekinesis);
            }

            void ShowDropdown()
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Camera"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.Camera, () =>
                {
                    text = "Camera";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.Camera;
                });
                menu.AddItem(new GUIContent("World"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.World, () =>
                {
                    text = "World";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.World;
                });
                menu.AddItem(new GUIContent("Parent"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.Parent, () =>
                {
                    text = "Parent";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.Parent;
                });
                menu.AddItem(new GUIContent("Local"), Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.Local, () =>
                {
                    text = "Local";
                    Settings.CoordSys = SpaceNavigatorDriver.CoordinateSystem.Local;
                });
                menu.ShowAsContext();
            }
        }
    }
}