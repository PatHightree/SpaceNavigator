using System;
using System.Threading;
using MonoLibUsb;
using Usb = MonoLibUsb.MonoUsbApi;
using UnityEngine;

public class SpaceNavigator {
	public enum CoordSys {
		World, Camera, Self,
	}

	// Singleton stuff.
	public static SpaceNavigator Instance {
		get { return _instance ?? (_instance = new SpaceNavigator()); }
	}
	private static SpaceNavigator _instance;
	public static bool HasInstance {
		get { return _instance != null; }
	}
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

	// Public API.
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
	public Transform Target;
	public CoordSys CoordinateSystem;
	public float TranslationSensitivity = 0.0005f, RotationSensitivity = 0.005f;
	public int ReadIntervalMs = 40;	// 25Hz

	// Device reading properties.
	public MonoUsbSessionHandle Session {
		get {
			if (ReferenceEquals(_sessionHandle, null))
				_sessionHandle = new MonoUsbSessionHandle();
			return _sessionHandle;
		}
	}
	private MonoUsbSessionHandle _sessionHandle;
	private MonoUsbDeviceHandle _deviceHandle;
	private readonly Thread _workerThread;
	private bool _quit;
	private const int ReadBufferLen = 64;
	private readonly byte[] _readBuffer = new byte[ReadBufferLen];
	private const int Timeout = 10;

	// Device parameters.
	private const int Config = 1;
	private const byte Endpoint = 0x81;
	private const int Interface = 0;
	private const short SpaceNavigatorVendorID = 0x046d;
	private const short SpaceNavigatorProductID = -14810;	// Should be 0xc626, which only fits in an unsigned short but MonoUsbApi.OpenDeviceWithVidPid doesn't take that.

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
		int r = MonoUsbApi.SetConfiguration(_deviceHandle, Config);
		if (r != 0) return false;

		// Claim interface
		MonoUsbApi.ClaimInterface(_deviceHandle, Interface);
		return true;
	}
	/// <summary>
	/// Reads data from the SpaceNavigator (blocking).
	/// </summary>
	private void ReadSpaceNavigator() {
		int transferred;

		int r = MonoUsbApi.BulkTransfer(_deviceHandle,
										Endpoint,
										_readBuffer,
										ReadBufferLen,
										out transferred,
										Timeout);
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
				MonoUsbApi.ReleaseInterface(_deviceHandle, Interface);
				_deviceHandle.Close();
			}
		}
		if (Session != null)
			Session.Close();
	}
}
