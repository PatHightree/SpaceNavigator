using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        private class PresentationMode : EditorToolbarToggle
        {
            public const string ID = "SpaceNavigator/PresentationMode";
            public PresentationMode()
            {
                onIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "PresentationModeOn.psd");
                offIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "PresentationModeOff.psd");
                tooltip = "Presentation Mode";
                this.RegisterValueChangedCallback(Test);
            }

            private void Test(ChangeEvent<bool> evt)
            {
                Settings.PresentationMode = evt.newValue;
            }
        }
    }
}