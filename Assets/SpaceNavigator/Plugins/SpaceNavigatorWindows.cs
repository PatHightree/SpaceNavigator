using System;
using System.Runtime.InteropServices;
using TDx.TDxInput;
using UnityEngine;

class SpaceNavigatorWindows : SpaceNavigator {	
	// Public API
	public override Vector3 GetTranslation() {
		float sensitivity = Application.isPlaying ? PlayTransSens : TransSens;
		return (SubInstance._sensor == null ?
			Vector3.zero :
			new Vector3(
				LockTranslationX || LockTranslationAll ? 0 : (float)SubInstance._sensor.Translation.X,
				LockTranslationY || LockTranslationAll ? 0 : (float)SubInstance._sensor.Translation.Y,
				LockTranslationZ || LockTranslationAll ? 0 : -(float)SubInstance._sensor.Translation.Z) *
				sensitivity * TransSensScale);
	}
	public override Quaternion GetRotation() {
		float sensitivity = Application.isPlaying ? PlayRotSens : RotSens;
		return (SubInstance._sensor == null ?
			Quaternion.identity :
			Quaternion.AngleAxis(
				(float)SubInstance._sensor.Rotation.Angle * sensitivity * RotSensScale,
				new Vector3(
					LockRotationX || LockRotationAll ? 0 : -(float)SubInstance._sensor.Rotation.X,
					LockRotationY || LockRotationAll ? 0 : -(float)SubInstance._sensor.Rotation.Y,
					LockRotationZ || LockRotationAll ? 0 : (float)SubInstance._sensor.Rotation.Z)));
	}

	// Device variables
	private readonly Sensor _sensor;
	private readonly Device _device;
	//private readonly Keyboard _keyboard;

	#region - Singleton -
	/// <summary>
	/// Private constructor, prevents a default instance of the <see cref="SpaceNavigatorWindows" /> class from being created.
	/// </summary>
	private SpaceNavigatorWindows() {
		try {
			if (_device == null) {
				_device = new DeviceClass();
				_sensor = _device.Sensor;
				//_keyboard = _device.Keyboard;
			}
			if (!_device.IsConnected)
				_device.Connect();
		}
		catch (COMException ex) {
			Debug.LogError(ex.ToString());
		}
	}

	public static SpaceNavigatorWindows SubInstance {
		get { return _subInstance ?? (_subInstance = new SpaceNavigatorWindows()); }
	}
	private static SpaceNavigatorWindows _subInstance;
	#endregion - Singleton -

	#region - IDisposable -
	public override void Dispose() {
		try {
			if (_device != null && _device.IsConnected) {
				_device.Disconnect();
				_subInstance = null;
				GC.Collect();
			}
		}
		catch (COMException ex) {
			Debug.LogError(ex.ToString());
		}
	}
	#endregion - IDisposable -
}
