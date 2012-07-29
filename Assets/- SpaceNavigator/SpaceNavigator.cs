using System;
using System.Threading;
using MonoLibUsb;
using MonoLibUsb.Profile;
using Usb = MonoLibUsb.MonoUsbApi;
using UnityEngine;

public class SpaceNavigator {
	// Singleton stuff.
	public static SpaceNavigator Instance {
		get { return _instance ?? (_instance = new SpaceNavigator()); }
	}
	private static SpaceNavigator _instance;
	/// <summary>
	/// Prevents a default instance of the <see cref="SpaceNavigator" /> class from being created.
	/// </summary>
	private SpaceNavigator() {
		_translation = _rotation = Vector3.zero;
		_workerThread = new Thread(SpaceNavigatorThread);
		_workerThread.Start();
	}
	~SpaceNavigator() {
		_quit = true;
	}

	public Vector3 Translation {
		get {
			return (_translation);
		}
	}
	private Vector3 _translation;
	public Vector3 Rotation{
		get {
			return (_rotation);
		}
	}
	private Vector3 _rotation;
	public bool HasNewData;

	// Threading properties.
	private readonly Thread _workerThread;
	private bool _quit;

	public MonoUsbSessionHandle Session {
		get {
			if (ReferenceEquals(_sessionHandle, null))
				_sessionHandle = new MonoUsbSessionHandle();
			return _sessionHandle;
		}
	}
	private MonoUsbSessionHandle _sessionHandle;
	private MonoUsbDeviceHandle _deviceHandle;

	// ReSharper disable InconsistentNaming
	private const int READ_BUFFER_LEN = 64;
	private const int MY_TIMEOUT = 10;
	// ReSharper restore InconsistentNaming

	private MonoUsbProfileList _profileList;

	public Transform Target;
	public CoordinateSystem CoordinateSystem;
	public float TranslationSensitivity = 0.001f, RotationSensitivity = 0.001f;
	public int ReadIntervalMs = 40;	// 25Hz
	private readonly byte[] _readBuffer = new byte[READ_BUFFER_LEN];

	#region - Device parameters -
	// ReSharper disable InconsistentNaming
	private const int MY_CONFIG = 1;
	private const byte MY_EP_READ = 0x81;
	private const byte MY_EP_WRITE = 0x01;
	private const int MY_INTERFACE = 0;
	private const short MY_PID = 1133;
	private const short MY_VID = -14810;
	//private const int SpaceNavigatorVendorID = 0x046d;
	//private const int SpaceNavigatorProductID = 0xc626;
	private const short SpaceNavigatorVendorID = 1133;
	private const short SpaceNavigatorProductID = -14810;
	// ReSharper restore InconsistentNaming
	#endregion - Device parameters -

	/// <summary>
	/// Worker thread to read data from SpaceNavigator.
	/// </summary>
	private void SpaceNavigatorThread() {
		if (!InitializeSpaceNavigator()) return;

		while (!_quit) {
			ReadSpaceNavigator();
			Thread.Sleep(ReadIntervalMs);
		}

		CloseSpaceNavigator();
	}

	/// <summary>
	/// Initializes the SpaceNavigator.
	/// </summary>
	/// <returns></returns>
	private bool InitializeSpaceNavigator() {
		_deviceHandle = MonoUsbApi.OpenDeviceWithVidPid(Session, SpaceNavigatorVendorID, SpaceNavigatorProductID);
		if ((_deviceHandle == null) || _deviceHandle.IsInvalid) return false;

		// Set configuration
		int r = MonoUsbApi.SetConfiguration(_deviceHandle, MY_CONFIG);
		if (r != 0) return false;

		// Claim interface
		MonoUsbApi.ClaimInterface(_deviceHandle, MY_INTERFACE);
		return true;
	}
	/// <summary>
	/// Reads data from the SpaceNavigator (blocking).
	/// </summary>
	private void ReadSpaceNavigator() {
		int transferred;

		int r = MonoUsbApi.BulkTransfer(_deviceHandle,
										MY_EP_READ,
										_readBuffer,
										READ_BUFFER_LEN,
										out transferred,
										MY_TIMEOUT);
		if (r == (int)MonoUsbError.ErrorTimeout) {
			// Timeout, this is considered normal operation
			_translation = Vector3.zero;
			_rotation = Vector3.zero;
			HasNewData = false;
		} else {
			if (r != 0) {
				// An error, other than ErrorTimeout was received. 
				D.error("Read failed:{0}", (MonoUsbError)r);
			} else {
				HasNewData = true;
				switch (_readBuffer[0]) {
					case 0x01:
						if (transferred != 7) break; // something is wrong
						_translation.x = (_readBuffer[1] + (sbyte)_readBuffer[2] * 256);
						_translation.z = (_readBuffer[3] + (sbyte)_readBuffer[4] * 256) * -1;
						_translation.y = (_readBuffer[5] + (sbyte)_readBuffer[6] * 256) * -1;
						_translation *= TranslationSensitivity;
						break;
					case 0x02:
						if (transferred != 7) break; // something is wrong
						_rotation.x = (_readBuffer[1] + (sbyte)_readBuffer[2] * 256) * -1;
						_rotation.z = (_readBuffer[3] + (sbyte)_readBuffer[4] * 256);
						_rotation.y = (_readBuffer[5] + (sbyte)_readBuffer[6] * 256);
						_rotation *= RotationSensitivity;
						break;
				}
			}
		}
		GC.Collect();
	}
	/// <summary>
	/// Closes the SpaceNavigator session and frees resources.
	/// </summary>
	private void CloseSpaceNavigator() {
		// Free and close resources
		if (_deviceHandle != null) {
			if (!_deviceHandle.IsInvalid) {
				MonoUsbApi.ReleaseInterface(_deviceHandle, MY_INTERFACE);
				_deviceHandle.Close();
			}
		}
		if (Session != null)
			Session.Close();
	}
}
