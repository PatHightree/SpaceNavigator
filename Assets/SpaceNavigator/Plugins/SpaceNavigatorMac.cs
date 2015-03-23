using System;
using UnityEngine;

public class SpaceNavigatorMac : SpaceNavigator
{
	// Public API
	public override Vector3 GetTranslation() {
		return Vector3.zero;
	}
	public override Quaternion GetRotation() {
		return Quaternion.identity;
	}

	#region - Singleton -
	/// <summary>
	/// Private constructor, prevents a default instance of the <see cref="SpaceNavigatorWindows" /> class from being created.
	/// </summary>
	private SpaceNavigatorMac() {
		try {
		}
		catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}
	
	public static SpaceNavigatorMac SubInstance {
		get { return _subInstance ?? (_subInstance = new SpaceNavigatorMac()); }
	}
	private static SpaceNavigatorMac _subInstance;
	#endregion - Singleton -
	
	#region - IDisposable -
	public override void Dispose() {
		try {
		}
		catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}
	#endregion - IDisposable -
}


