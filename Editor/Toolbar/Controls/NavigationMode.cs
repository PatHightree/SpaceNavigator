using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        private class NavigationMode : EditorToolbarDropdown
        {
            public const string ID = "SpaceNavigator/NavigationMode";

            public NavigationMode()
            {
                text = Settings.Mode.ToString();
                clicked += ShowDropDown;
            }

            private void ShowDropDown()
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Fly"), Settings.Mode == OperationMode.Fly, () =>
                {
                    text = "Fly";
                    Settings.Mode = OperationMode.Fly;
                });
                menu.AddItem(new GUIContent("Orbit"), Settings.Mode == OperationMode.Orbit, () =>
                {
                    text = "Orbit";
                    Settings.Mode = OperationMode.Orbit;
                });
                menu.AddItem(new GUIContent("Telekinesis"), Settings.Mode == OperationMode.Telekinesis, () =>
                {
                    text = "Telekinesis";
                    Settings.Mode = OperationMode.Telekinesis;
                });
                menu.AddItem(new GUIContent("GrabMove"), Settings.Mode == OperationMode.GrabMove, () =>
                {
                    text = "GrabMove";
                    Settings.Mode = OperationMode.GrabMove;
                });
                menu.ShowAsContext();
            }
        }
    }
}