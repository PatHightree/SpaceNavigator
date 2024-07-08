using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceNavigatorDriver
{
    [Overlay(typeof(SceneView), "SpaceNavigator")]
    [Icon(IconPath + "SpaceNavigator.png")]
    public partial class SpaceNavigatorToolbar : ToolbarOverlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        public static SpaceNavigatorToolbar Instance;
        
        private const bool Debug = true;
        private const string IconPath = "Packages/com.pathightree.spacenavigator-driver/Editor/Toolbar/Icons/";
        private static List<SpeedGearButton> m_speedGearButtons = new List<SpeedGearButton>();

        public static event EventHandler RefreshLayout;
        
        SpaceNavigatorToolbar()
        {
            Instance = this;
            RefreshLayout += (sender, args) =>
            {
                // Toggling the 'displayed' property causes the internal method 'RebuildContent' to be called
                displayed = false;
                displayed = true;
            };
        }

        private static OverlayToolbar CreateToolbar()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            toolbar.Add(new NavigationMode());
            if (Settings.ShowSpeedGearsAsRadioButtons)
            {
                m_speedGearButtons.Clear();
                m_speedGearButtons.Add(new SpeedGearButton(2, "SpeedGear 1.psd", "Minuscule Sensitivity", m_speedGearButtons));
                m_speedGearButtons.Add(new SpeedGearButton(1, "SpeedGear 2.psd", "Human Sensitivity", m_speedGearButtons));
                m_speedGearButtons.Add(new SpeedGearButton(0, "SpeedGear 3.psd", "Huge Sensitivity", m_speedGearButtons));
                m_speedGearButtons.ForEach(b => toolbar.Add(b));
            }
            else
                toolbar.Add(new SpeedGearDropdown());

            toolbar.Add(new PresentationMode());
            bool showSnapButtons = Settings.Mode == OperationMode.Telekinesis || Settings.Mode == OperationMode.GrabMove;
            if (showSnapButtons)
            {
                toolbar.Add(new SnapGrid());
                toolbar.Add(new SnapAngle());
            }   
            if (Debug)
                toolbar.Add(new ShowSettings());
            EditorToolbarUtility.SetupChildrenAsButtonStrip(toolbar);
            return toolbar;
        }

        public override VisualElement CreatePanelContent()
        {
            return CreateToolbar();
        }

        public new OverlayToolbar CreateHorizontalToolbarContent()
        {
            return CreateToolbar();
        }

        public new OverlayToolbar CreateVerticalToolbarContent()
        {
            return CreateToolbar();
        }

        public void TriggerRefresh()
        {
            RefreshLayout?.Invoke(this, EventArgs.Empty);
        }
    }
}