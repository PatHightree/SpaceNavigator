using System;
using System.Runtime.InteropServices;
using System.Threading;
using MonoLibUsb;
using MonoLibUsb.Profile;
using MonoLibUsb.Transfer;
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
		Lock = new object();
		_workerThread = new Thread(SpaceNavigatorThread);
		_workerThread.Start();
	}
	~SpaceNavigator() {
		_quit = true;
	}

	public Vector3 Translation {
		get {
			//lock(Lock) {
				return (_translation);
			//}
		}
	}
	private Vector3 _translation;
	//private bool _gotTranslation;
	public Vector3 Rotation{
		get {
			//lock (Lock) {
				return (_rotation);
			//}
		}
	}
	private Vector3 _rotation;
	//private bool _gotRotation;
	public object Lock;

	// Threading properties.
	private Thread _workerThread;
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
	private const int TEST_READ_LEN = 64;
	private const int TEST_WRITE_LEN = 8;
	private const int MY_TIMEOUT = 2000;
	// ReSharper restore InconsistentNaming

	private MonoUsbProfileList _profileList;

	public Transform Target;
	public CoordinateSystem CoordinateSystem;
	public float TranslationSensitivity = 0.001f, RotationSensitivity = 0.001f;
	public int ReadIntervalMs = 40;	// 25Hz
	public byte[] ReadBuffer = new byte[TEST_READ_LEN];

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

	private void SpaceNavigatorThread() {
		if (!InitializeSpaceNavigator()) return;

		while (!_quit) {
			//lock (Lock) {
				ReadSpaceNavigator(ref _translation, ref _rotation, ref ReadBuffer);
			//}
			Thread.Sleep(ReadIntervalMs);
		}

		CloseSpaceNavigator();
	}
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

	private void ReadSpaceNavigator(ref Vector3 translation, ref Vector3 rotation, ref byte[] readBuffer) {
		int transferred;

		int r = MonoUsbApi.BulkTransfer(_deviceHandle,
										MY_EP_READ,
										readBuffer,
										TEST_READ_LEN,
										out transferred,
										MY_TIMEOUT);
		if (r == (int)MonoUsbError.ErrorTimeout) {
			// This is considered normal operation
			//D.log("Read Timed Out. {0} packet(s) read ({1} bytes)", PacketCount, TransferredTotal);
		} else {
			if (r != 0) {
				// An error, other than ErrorTimeout was received. 
				D.log("Read failed:{0}", (MonoUsbError)r);
			} else {
				switch (readBuffer[0]) {
					case 0x01:
						if (transferred != 7) break; // something is wrong
						translation.x = (readBuffer[1] + (sbyte)readBuffer[2] * 256);
						translation.z = (readBuffer[3] + (sbyte)readBuffer[4] * 256) * -1;
						translation.y = (readBuffer[5] + (sbyte)readBuffer[6] * 256) * -1;
						translation *= TranslationSensitivity;
						//_gotTranslation = true;
						break;
					case 0x02:
						if (transferred != 7) break; // something is wrong
						rotation.x = (readBuffer[1] + (sbyte)readBuffer[2] * 256) * -1;
						rotation.z = (readBuffer[3] + (sbyte)readBuffer[4] * 256);
						rotation.y = (readBuffer[5] + (sbyte)readBuffer[6] * 256);
						rotation *= RotationSensitivity;
						//_gotRotation = true;
						break;
					default:
						break;
				}
			}
		}
		GC.Collect();
	}

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

	// This function originated from bulk_transfer_cb()
	// in sync.c of the Libusb-1.0 source code.
	private static void bulkTransferCB(MonoUsbTransfer transfer) {
		Marshal.WriteInt32(transfer.PtrUserData, 1);
		/* caller interprets results and frees transfer */
	}
}
