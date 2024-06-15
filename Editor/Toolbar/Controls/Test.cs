using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        private class Test : EditorToolbarButton
        {
            public const string ID = "SpaceNavigator/Test";

            public Test()
            {
                // text = "Test";
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "Test.png");
                tooltip = "Test";
                clicked += Onclicked;
            }

            private void Onclicked()
            {
            }
        }
    }
}