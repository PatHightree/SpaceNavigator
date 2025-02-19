#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        private class SnapGrid : EditorToolbarToggle
        {
            public const string ID = "SpaceNavigator/SnapGrid";

            public SnapGrid()
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "Snap Grid.psd");
                tooltip = "Snap Grid";
                this.RegisterValueChangedCallback(Test);
                MayBeVisible();
            }
            
            private void Test(ChangeEvent<bool> evt)
            {                
                Settings.SnapTranslation = evt.newValue;
            }

            private void MayBeVisible()
            {
                SetEnabled(Settings.Mode == OperationMode.Telekinesis || Settings.Mode == OperationMode.GrabMove);
            }
        }
    }
}
#endif