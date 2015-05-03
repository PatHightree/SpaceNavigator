using System;
using UnityEngine;
using System.Runtime.InteropServices;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
public class SpaceNavigatorMac : SpaceNavigator {
	private const float TransSensScale = 0.007f, RotSensScale = 0.025f;

#if UNITY_4_0 || UNITY_4_1 ||  UNITY_4_2 ||  UNITY_4_3 ||  UNITY_4_4 ||  UNITY_4_5 ||  UNITY_4_6
	private const string BundleName = "3DConnexionWrapperU4";
#else
	private const string BundleName = "3DConnexionWrapperU5";
#endif
	[DllImport(BundleName)]
	private static extern int InitDevice();
	[DllImport(BundleName)]
	private static extern void SampleTranslation(ref int x, ref int y, ref int z);
	[DllImport(BundleName)]
	private static extern void SampleRotation(ref int rx, ref int ry, ref int rz);
	[DllImport(BundleName)]
	private static extern int DisposeDevice();

	private int _clientID;

	// Public API
	public override Vector3 GetTranslation() {
		int x = 0, y = 0, z = 0;
		SampleTranslation(ref x, ref y, ref z);
		float sensitivity = Application.isPlaying ? PlayTransSens : TransSens[CurrentGear];
		return (
				   _clientID == 0 ?
					   Vector3.zero :
					   new Vector3(
						   LockTranslationX || LockTranslationAll ? 0 : (float)x,
						   LockTranslationY || LockTranslationAll ? 0 : -(float)z,
						   LockTranslationZ || LockTranslationAll ? 0 : -(float)y) * sensitivity * TransSensScale);
	}
	public override Quaternion GetRotation() {
		int rx = 0, ry = 0, rz = 0;
		SampleRotation(ref rx, ref ry, ref rz);
		float sensitivity = Application.isPlaying ? PlayRotSens : RotSens;
		return (
				   _clientID == 0 ?
					   Quaternion.identity :
					   Quaternion.Euler(
						   new Vector3(
							   LockRotationX || LockRotationAll ? 0 : -(float)rx,
							   LockRotationY || LockRotationAll ? 0 : (float)rz,
							   LockRotationZ || LockRotationAll ? 0 : (float)ry) * sensitivity * RotSensScale));
	}

	#region - Singleton -
	/// <summary>
	/// Private constructor, prevents a default instance of the <see cref="SpaceNavigatorMac" /> class from being created.
	/// </summary>
	private SpaceNavigatorMac() {
		_clientID = 0;
		try {
			_clientID = InitDevice();
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
			DisposeDevice();
			_clientID = 0;
		}
		catch (Exception ex) {
			Debug.LogError(ex.ToString());
		}
	}
	#endregion - IDisposable -
}

#endif // UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
