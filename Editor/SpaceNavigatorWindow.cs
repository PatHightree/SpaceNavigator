#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace SpaceNavigatorDriver {

	[Serializable]
	public class SpaceNavigatorWindow : EditorWindow, IDisposable {

		/// <summary>
		/// Initializes the window.
		/// </summary>
		[MenuItem("Window/SpaceNavigator/Settings &s", false, 1)]
		public static void Init() {
			SpaceNavigatorWindow window = GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;
			if (window) {
				window.titleContent = new GUIContent("SpaceNavigator");
				window.Show();
			}
		}

		[MenuItem("Window/SpaceNavigator/Reset Window Position", false, 2)]
		public static void ResetPosition() {
			SpaceNavigatorWindow window = GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;
			if (window)
			{
				Rect rect = window.position;
				rect.x = Screen.currentResolution.width / 2;
				rect.y = Screen.currentResolution.height / 2;
				window.position = rect; 
				window.Show();
			}
		}

		public static void OnDisable() {
			// Write settings to PlayerPrefs when EditorWindow is closed.
			Settings.Write();
		}

		public static void OnDestroy() {
			// Write settings to PlayerPrefs when EditorWindow is closed.
			Settings.Write();
		}

		// This does not get called, unfortunately...
		public void OnApplicationQuit() {
			ViewportController.OnApplicationQuit();
		}

		public void OnSelectionChange() {
			ViewportController.StoreSelectionTransforms();
		}

		public void OnGUI() {
			if (Settings.OnGUI())
				SpaceNavigatorToolbar.Instance.TriggerRefresh();
		}

		public void Dispose() {
			// Write settings to PlayerPrefs when EditorWindow is closed.
			Settings.Write();
		}
	}
}
#endif