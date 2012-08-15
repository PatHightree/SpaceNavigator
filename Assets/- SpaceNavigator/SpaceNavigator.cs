#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

using System;
using System.Runtime.InteropServices;
using System.Threading;
using LibUsbDotNet.Main;
using MonoLibUsb;
using MonoLibUsb.Profile;
using MonoLibUsb.Transfer;
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
	//private const ushort SpaceNavigatorProductID = 0xc626;

	private static MonoUsbTransferDelegate controlTransferDelegate;
	private static MonoUsbSessionHandle sessionHandle;

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

	// Predicate functions for finding only devices with the specified VendorID & ProductID.
	private static bool MyVidPidPredicate(MonoUsbProfile profile) {
		if (profile.DeviceDescriptor.VendorID == SpaceNavigatorVendorID && profile.DeviceDescriptor.ProductID == SpaceNavigatorProductID)
			return true;
		return false;
	}

	/// <summary>
	/// Initializes the SpaceNavigator.
	/// </summary>
	/// <returns></returns>
	private bool InitializeSpaceNavigator() {
		// Assign the control transfer delegate to the callback function. 
		controlTransferDelegate = ControlTransferCB;

		// Initialize the context.
		sessionHandle = new MonoUsbSessionHandle();
		if (sessionHandle.IsInvalid)
			throw new Exception(String.Format("Failed intializing libusb context.\n{0}:{1}",
											  MonoUsbSessionHandle.LastErrorCode,
											  MonoUsbSessionHandle.LastErrorString));

		MonoUsbProfileList profileList = new MonoUsbProfileList();
		MonoUsbDeviceHandle myDeviceHandle = null;

		try {
			// The list is initially empty.
			// Each time refresh is called the list contents are updated. 
			profileList.Refresh(sessionHandle);

			// Use the GetList() method to get a generic List of MonoUsbProfiles
			// Find the first profile that matches in MyVidPidPredicate.
			MonoUsbProfile myProfile = profileList.GetList().Find(MyVidPidPredicate);
			if (myProfile == null) {
				Debug.Log("Device not connected.");
				return false;
			} else
				Debug.Log("Found it");

			// Open the device handle to perform I/O
			myDeviceHandle = myProfile.OpenDeviceHandle();
			if (myDeviceHandle.IsInvalid)
				throw new Exception(String.Format("Failed opening device handle.\n{0}:{1}",
												  MonoUsbDeviceHandle.LastErrorCode,
												  MonoUsbDeviceHandle.LastErrorString));
			int ret;
			MonoUsbError e;

			// Set Configuration
			e = (MonoUsbError)(ret = MonoUsbApi.SetConfiguration(myDeviceHandle, 1));
			if (ret < 0) throw new Exception(String.Format("Failed SetConfiguration.\n{0}:{1}", e, MonoUsbApi.StrError(e)));

			// Claim Interface
			e = (MonoUsbError)(ret = MonoUsbApi.ClaimInterface(myDeviceHandle, 0));
			if (ret < 0) throw new Exception(String.Format("Failed ClaimInterface.\n{0}:{1}", e, MonoUsbApi.StrError(e)));

			// Create a vendor specific control setup, allocate 1 byte for return control data.
			byte requestType = (byte)(UsbCtrlFlags.Direction_In | UsbCtrlFlags.Recipient_Device | UsbCtrlFlags.RequestType_Vendor);
			byte request = 0x0F;
			MonoUsbControlSetupHandle controlSetupHandle = new MonoUsbControlSetupHandle(requestType, request, 0, 0, 1);

			// Transfer the control setup packet
			ret = libusb_control_transfer(myDeviceHandle, controlSetupHandle, 1000);
			if (ret > 0) {
				Debug.Log("\nSuccess!\n");
				byte[] ctrlDataBytes = controlSetupHandle.ControlSetup.GetData(ret);
				string ctrlDataString = Helper.HexString(ctrlDataBytes, String.Empty, "h ");
				D.log("Return Length: {0}", ret);
				D.log("DATA (hex)   : [ {0} ]\n", ctrlDataString.Trim());
			}
			MonoUsbApi.ReleaseInterface(myDeviceHandle, 0);
		} finally {
			profileList.Close();
			if (myDeviceHandle != null) myDeviceHandle.Close();
			sessionHandle.Close();
		}

		return true;
	}

	private static void ControlTransferCB(MonoUsbTransfer transfer) {
		ManualResetEvent completeEvent = GCHandle.FromIntPtr(transfer.PtrUserData).Target as ManualResetEvent;
		completeEvent.Set();
	}

	private static int libusb_control_transfer(MonoUsbDeviceHandle deviceHandle, MonoUsbControlSetupHandle controlSetupHandle, int timeout) {
		MonoUsbTransfer transfer = MonoUsbTransfer.Alloc(0);
		ManualResetEvent completeEvent = new ManualResetEvent(false);
		GCHandle gcCompleteEvent = GCHandle.Alloc(completeEvent);

		transfer.FillControl(deviceHandle, controlSetupHandle, controlTransferDelegate, GCHandle.ToIntPtr(gcCompleteEvent), timeout);

		int r = (int)transfer.Submit();
		if (r < 0) {
			transfer.Free();
			gcCompleteEvent.Free();
			return r;
		}

		while (!completeEvent.WaitOne(0, false)) {
			r = MonoUsbApi.HandleEvents(sessionHandle);
			if (r < 0) {
				if (r == (int)MonoUsbError.ErrorInterrupted)
					continue;
				transfer.Cancel();
				while (!completeEvent.WaitOne(0, false))
					if (MonoUsbApi.HandleEvents(sessionHandle) < 0)
						break;
				transfer.Free();
				gcCompleteEvent.Free();
				return r;
			}
		}

		if (transfer.Status == MonoUsbTansferStatus.TransferCompleted)
			r = transfer.ActualLength;
		else
			r = (int)MonoUsbApi.MonoLibUsbErrorFromTransferStatus(transfer.Status);

		transfer.Free();
		gcCompleteEvent.Free();
		return r;
	}

	/// <summary>
	/// Reads data from the SpaceNavigator (blocking).
	/// </summary>
	private void ReadSpaceNavigator() {
		//int transferred;

		//int r = MonoUsbApi.BulkTransfer(_deviceHandle,
		//								Endpoint,
		//								_readBuffer,
		//								ReadBufferLen,
		//								out transferred,
		//								Timeout);
		//if (r == (int)MonoUsbError.ErrorTimeout) {
		//	// Timeout, this is considered normal operation
		//	_translation = Vector3.zero;
		//	_rotation = Vector3.zero;
		//	HasNewData = false;
		//} else {
		//	if (r != 0) {
		//		// An error, other than ErrorTimeout was received. 
		//		D.error("Read failed:{0}", (MonoUsbError)r);
		//	} else {
		//		HasNewData = true;
		//		switch (_readBuffer[0]) {
		//			case 0x01:
		//				if (transferred != 7) break; // something is wrong
		//				_translation.x = (_readBuffer[1] + (sbyte)_readBuffer[2] * 256);
		//				_translation.z = (_readBuffer[3] + (sbyte)_readBuffer[4] * 256) * -1;
		//				_translation.y = (_readBuffer[5] + (sbyte)_readBuffer[6] * 256) * -1;
		//				_translation *= TranslationSensitivity;
		//				break;
		//			case 0x02:
		//				if (transferred != 7) break; // something is wrong
		//				_rotation.x = (_readBuffer[1] + (sbyte)_readBuffer[2] * 256) * -1;
		//				_rotation.z = (_readBuffer[3] + (sbyte)_readBuffer[4] * 256);
		//				_rotation.y = (_readBuffer[5] + (sbyte)_readBuffer[6] * 256);
		//				_rotation *= RotationSensitivity;
		//				break;
		//		}
		//	}
		//}
		//GC.Collect();
	}
	/// <summary>
	/// Closes the SpaceNavigator session and frees resources.
	/// </summary>
	private void CloseSpaceNavigator() {
		//// Free and close resources
		//if (_deviceHandle != null) {
		//	if (!_deviceHandle.IsInvalid) {
		//		MonoUsbApi.ReleaseInterface(_deviceHandle, Interface);
		//		_deviceHandle.Close();
		//	}
		//}
		//if (Session != null)
		//	Session.Close();
	}
}
