#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

using System;
using System.Runtime.InteropServices;
using TDx.TDxInput;
using UnityEngine;

public class SpaceNavigator : IDisposable {
	public enum CoordSys {
		World, Camera, Self,
	}

	public CoordSys CoordinateSystem;
	public float TranslationSensitivity = 0.0005f, RotationSensitivity = 0.005f;

	private readonly Sensor _sensor;
	private readonly Device _device;
	//private Keyboard _keyboard;

	#region - Singleton stuff -
	/// <summary>
	/// Private constructor, prevents a default instance of the <see cref="SpaceNavigator" /> class from being created.
	/// </summary>
	private SpaceNavigator() {
		try {
			if (_device == null) {
				_device = new DeviceClass();
				_sensor = _device.Sensor;
				//_keyboard = _device.Keyboard;
				_device.LoadPreferences("Unity");
			}
			if (!_device.IsConnected)
				_device.Connect();
		}
		catch (COMException ex) {
			Debug.Log(ex.ToString());
		}
		Debug.Log("Initialized");
	}

	public static SpaceNavigator Instance {
		get { return _instance ?? (_instance = new SpaceNavigator()); }
	}
	private static SpaceNavigator _instance;
	public static bool HasInstance {
		get { return _instance != null; }
	}
	#endregion - Singleton stuff -

	#region - IDisposable -
	public void Dispose() {
		try {
			if (_device != null && _device.IsConnected) {
				_device.Disconnect();
				_instance = null;
				GC.Collect();
				Debug.Log("Disconnected");
			}
		}
		catch (COMException ex) {
			Debug.Log(ex.ToString());
		}
	}
	#endregion - IDisposable -

	// Public API.
	public Vector3 Translation {
		get {
			return (
				new Vector3(
					(float)_sensor.Translation.X, 
					(float)_sensor.Translation.Y, 
					-(float)_sensor.Translation.Z) *
					TranslationSensitivity);
		}
	}
	public Vector3 Rotation{
		get {
			return (
				new Vector3(
					-(float)_sensor.Rotation.X, 
					-(float)_sensor.Rotation.Y, 
					(float)_sensor.Rotation.Z) *
					RotationSensitivity);
		}
	}
}
