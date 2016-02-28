using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceNavigatorDriver {

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
	using TDx.TDxInput;

	class SpaceNavigatorWindows : SpaceNavigator {
		private const float TransSensScale = 0.0001f, RotSensScale = 0.0008f;

		// Public API
		public override Vector3 GetTranslation() {
			float sensitivity = Application.isPlaying ? Settings.PlayTransSens : Settings.TransSens[Settings.CurrentGear];
			return (SubInstance._sensor == null ?
						Vector3.zero :
						new Vector3(
							Settings.GetLock(DoF.Translation, Axis.X) ? 0 : (float)SubInstance._sensor.Translation.X,
							Settings.GetLock(DoF.Translation, Axis.Y) ? 0 : (float)SubInstance._sensor.Translation.Y,
							Settings.GetLock(DoF.Translation, Axis.Z) ? 0 : -(float)SubInstance._sensor.Translation.Z) *
						sensitivity * TransSensScale);
		}
		public override Quaternion GetRotation() {
			float sensitivity = Application.isPlaying ? Settings.PlayRotSens : Settings.RotSens;
			return (SubInstance._sensor == null ?
						Quaternion.identity :
						Quaternion.AngleAxis(
							(float)SubInstance._sensor.Rotation.Angle * sensitivity * RotSensScale,
							new Vector3(
								Settings.GetLock(DoF.Rotation, Axis.X) ? 0 : -(float)SubInstance._sensor.Rotation.X,
								Settings.GetLock(DoF.Rotation, Axis.Y) ? 0 : -(float)SubInstance._sensor.Rotation.Y,
								Settings.GetLock(DoF.Rotation, Axis.Z) ? 0 : (float)SubInstance._sensor.Rotation.Z)));
		}

		// Device variables
		private Sensor _sensor;
		private Device _device;
		//private Keyboard _keyboard;

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
#endif    // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
}