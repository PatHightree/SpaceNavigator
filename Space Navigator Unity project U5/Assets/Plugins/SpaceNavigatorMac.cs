using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class SpaceNavigatorMac : SpaceNavigator
{
	[DllImport ("XCodePlugin")]
	private static extern int InitDevice();
	[DllImport ("XCodePlugin")]
	private static extern int SampleDevice();
	[DllImport ("XCodePlugin")]
	private static extern void DisposeDevice();

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
			//InitDevice();
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
			//DisposeDevice();
		}
		catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}
	#endregion - IDisposable -
}


