using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        class SpeedGear : EditorToolbarDropdown
        {
            public const string ID = "SpaceNavigator/SpeedGear";

            private Texture2D m_gearMinuscule;
            private Texture2D m_gearHuman;
            private Texture2D m_gearHuge;

            public SpeedGear()
            {
                tooltip = "Sensitivity";
                m_gearMinuscule = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "SpeedGear 1.psd");
                m_gearHuman = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "SpeedGear 2.psd");
                m_gearHuge = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + "SpeedGear 3.psd");
                switch (Settings.CurrentGear)
                {
                     case 0: icon = m_gearHuge; break;
                     case 1: icon = m_gearHuman; break;
                     case 3: icon = m_gearMinuscule; break;
                }
                clicked += ShowDropdown;
            }

            void ShowDropdown()
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Minuscule"), Settings.CurrentGear == 2, () =>
                {
                    icon = m_gearMinuscule;
                    Settings.CurrentGear = 2;
                });
                menu.AddItem(new GUIContent("Human"), Settings.CurrentGear == 1, () =>
                {
                    icon = m_gearHuman;
                    Settings.CurrentGear = 1;
                });
                menu.AddItem(new GUIContent("Huge"), Settings.CurrentGear == 0, () =>
                {
                    icon = m_gearHuge;
                    Settings.CurrentGear = 0;
                });
                menu.ShowAsContext();
            }
        }
    }
}