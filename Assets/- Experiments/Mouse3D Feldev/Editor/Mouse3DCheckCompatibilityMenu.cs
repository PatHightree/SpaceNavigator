using UnityEngine;
using UnityEditor;

public class Mouse3DCheckCompatibilityMenu {

	[MenuItem("Window/Mouse 3D Check Compatibility")]
	static public void AddCheckPanel ()
	{
		EditorWindow.GetWindow<Mouse3DCheckCompatibility>(false, "Mouse 3D Check", true);
	}
}
