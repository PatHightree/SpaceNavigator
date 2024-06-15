using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        private class ShowSettings : EditorToolbarButton
        {
            public const string ID = "SpaceNavigator/ShowSettings";

            public ShowSettings()
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "Show Settings.psd");
                tooltip = "Show Settings";
                clicked += Onclicked;
            }

            private void Onclicked()
            {
                SpaceNavigatorWindow window = SpaceNavigatorWindow.GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;
            }
        }
    }
}