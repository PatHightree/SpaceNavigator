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
        private class HorizonLock : EditorToolbarToggle
        {
            public const string ID = "SpaceNavigator/HorizonLock";

            public HorizonLock()
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "Horizon Lock.psd");
                tooltip = "Horizon Lock";
                value = Settings.HorizonLock;
                this.RegisterValueChangedCallback(Test);
                MayBeVisible();
            }
            
            private void Test(ChangeEvent<bool> evt)
            {                
                Settings.HorizonLock = evt.newValue;
            }

            private void MayBeVisible()
            {
                SetEnabled(!(Settings.Mode == OperationMode.Telekinesis && Settings.CoordSys == SpaceNavigatorDriver.CoordinateSystem.World));
            }
        }
    }
}
#endif