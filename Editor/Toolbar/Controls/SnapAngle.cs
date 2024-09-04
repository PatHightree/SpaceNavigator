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
        private class SnapAngle : EditorToolbarToggle
        {
            public const string ID = "SpaceNavigator/SnapAngle";

            public SnapAngle()
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "Snap Angle.psd");
                tooltip = "Snap Angle";
                this.RegisterValueChangedCallback(Test);
                MayBeVisibe();
            }
            private void Test(ChangeEvent<bool> evt)
            {                
                Settings.SnapRotation = evt.newValue;
            }

            private void MayBeVisibe()
            {
                SetEnabled(Settings.Mode == OperationMode.Telekinesis || Settings.Mode == OperationMode.GrabMove);
            }
        }
    }
}
#endif