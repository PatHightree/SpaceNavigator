using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SpaceNavigatorDriver
{
    [Overlay(typeof(SceneView), "SpaceNavigator")]
    [Icon(IconPath + "SpaceNavigator.png")]
    public partial class SpaceNavigatorToolbar : ToolbarOverlay
    {
        private const string IconPath = "Packages/com.pathightree.spacenavigator-driver/Editor/Toolbar/Icons/";

        SpaceNavigatorToolbar() : base(
            Test.ID,
            NavigationMode.ID,
            DropdownToggleExample.ID,
            PresentationMode.ID)
        {
        }
    }
}