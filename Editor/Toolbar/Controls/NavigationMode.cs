using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceNavigatorDriver
{
    public partial class SpaceNavigatorToolbar
    {
        [EditorToolbarElement(ID, typeof(SceneView))]
        private class NavigationMode : VisualElement
        {
            public const string ID = "SpaceNavigator/NavigationMode";
            VisualElement m_navModeButtonsParent;
            private List<NavigationModeToggle> m_navModeToggles;

            public NavigationMode()
            {
                m_navModeToggles = new List<NavigationModeToggle>();
                m_navModeToggles.Add(new NavigationModeToggle(OperationMode.Fly, "NavigationMode Fly.png", "Fly", m_navModeToggles));
                m_navModeToggles.Add(new NavigationModeToggle(OperationMode.Orbit, "NavigationMode Orbit.png", "Orbit", m_navModeToggles));
                m_navModeToggles.Add(new NavigationModeToggle(OperationMode.Telekinesis, "NavigationMode Telekinesis.png", "Telekinesis", m_navModeToggles));
                m_navModeToggles.Add(new NavigationModeToggle(OperationMode.GrabMove, "NavigationMode GrabMove.png", "Grab Move", m_navModeToggles));
                
                m_navModeButtonsParent = new VisualElement() { name = "Builtin View and Transform Tools" };
                m_navModeButtonsParent.AddToClassList("toolbar-contents");
                m_navModeButtonsParent.Clear();
                m_navModeToggles.ForEach(nmt => m_navModeButtonsParent.Add(nmt));
                EditorToolbarUtility.SetupChildrenAsButtonStrip(m_navModeButtonsParent);
                Add(m_navModeButtonsParent);
                
                // Refresh button states when nav mode is changed in settings window
                Settings.ModeChanged += (sender, args) => m_navModeToggles.ForEach(t => t.SetValueWithoutNotify(t.Mode == Settings.Mode));
            }

            class NavigationModeToggle : EditorToolbarToggle
            {
                public OperationMode Mode;
                private List<NavigationModeToggle> m_toggles;

                public NavigationModeToggle(OperationMode _mode, string _iconName, string _tooltip, List<NavigationModeToggle> _toggles)
                {
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath + _iconName);
                    tooltip = _tooltip;
                    Mode = _mode;
                    m_toggles = _toggles;
                    this.RegisterValueChangedCallback(Test);
                    SetValueWithoutNotify(Mode == Settings.Mode);
                }
                private void Test(ChangeEvent<bool> evt)
                {
                    // Settings.Mode = Mode;
                    m_toggles.ForEach(t => t.SetValueWithoutNotify(t.Mode == Mode));
                    // Debug.Log(m_window.overlayCanvas == null);
                    // if (m_window.position) m_window.Repaint();
                }
            }
        }
    }
}