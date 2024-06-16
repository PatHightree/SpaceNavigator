using System;
using UnityEditor;
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
        private static SnapGrid m_snapGrid;
        private static SnapAngle m_snapAngle;
        private const bool Debug = true;
        private const string IconPath = "Packages/com.pathightree.spacenavigator-driver/Editor/Toolbar/Icons/";

        public static event EventHandler RefreshLayout;
        
        SpaceNavigatorToolbar()
        {
            RefreshLayout += (sender, args) =>
            {
                // How the heck do I refresh the layout of a toolbar ?!?!?
                
                // Close();
                // bool showSnapButtons = Settings.Mode == OperationMode.Telekinesis || Settings.Mode == OperationMode.GrabMove;
                // m_snapGrid.visible = showSnapButtons;
                // m_snapAngle.visible = showSnapButtons;
                // CreatePanelContent();
                // OnCreated();
            };
        }

        private static OverlayToolbar CreateToolbar()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            toolbar.Add(new NavigationMode());
            toolbar.Add(new SpeedGear());
            toolbar.Add(new PresentationMode());
            bool showSnapButtons = Settings.Mode == OperationMode.Telekinesis || Settings.Mode == OperationMode.GrabMove;
            if (showSnapButtons)
            {
                m_snapGrid = new SnapGrid();
                toolbar.Add(m_snapGrid);
                m_snapAngle = new SnapAngle();
                toolbar.Add(m_snapAngle);
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
    }
}