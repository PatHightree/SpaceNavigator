#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

using System;
using System.Linq;
using System.Runtime.InteropServices;
using MonoLibUsb;
using MonoLibUsb.Profile;
using MonoLibUsb.Transfer;
using Usb = MonoLibUsb.MonoUsbApi;
using UnityEngine;

public class MonoLibUsbExperiment : MonoBehaviour {
	public enum TestMode {
		Sync,
		Async
	}
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
	private TestMode TEST_MODE = TestMode.Sync;
	private const int TEST_READ_LEN = 64;
	private const int TEST_WRITE_LEN = 8;
	private const int MY_TIMEOUT = 2000;
	// ReSharper restore InconsistentNaming

	private MonoUsbProfileList _profileList;
	public int PacketCount = 0;
	public int TransferredTotal = 0;

	#region DEVICE SETUP
	// ReSharper disable InconsistentNaming
	private const int MY_CONFIG = 1;
	private const byte MY_EP_READ = 0x81;
	private const byte MY_EP_WRITE = 0x01;
	private const int MY_INTERFACE = 0;
	private const short MY_PID = 1133;
	private const short MY_VID = -14810;
	// ReSharper restore InconsistentNaming
	//private const int SpaceNavigatorVendorID = 0x046d;
	//private const int SpaceNavigatorProductID = 0xc626;
	private const short SpaceNavigatorVendorID = 1133;
	private const short SpaceNavigatorProductID = -14810;
	#endregion

	public void OnEnable() {
		_deviceHandle = MonoUsbApi.OpenDeviceWithVidPid(Session, SpaceNavigatorVendorID, SpaceNavigatorProductID);
		if ((_deviceHandle == null) || _deviceHandle.IsInvalid) return;

		// Set configuration
		D.log("Set Config..");
		int r = MonoUsbApi.SetConfiguration(_deviceHandle, MY_CONFIG);
		if (r != 0) return;

		// Claim interface
		D.log("Set Interface..");
		MonoUsbApi.ClaimInterface(_deviceHandle, MY_INTERFACE);

		PacketCount = 0;
		TransferredTotal = 0;
	}

	public void Update() {
		int r;
		int transferred;
		byte[] testReadData = new byte[TEST_READ_LEN];

		////////////////////
		// Read test data //
		////////////////////
		// If the Async TEST_MODE enumeration is set, use
		// the internal transfer function
		if (TEST_MODE == TestMode.Async) {
			r = (int)DoBulkAsyncTransfer(_deviceHandle,
											MY_EP_READ,
											testReadData,
											TEST_READ_LEN,
											out transferred,
											MY_TIMEOUT);
		} else {
			// Use the sync bulk transfer API function 
			r = MonoUsbApi.BulkTransfer(_deviceHandle,
										MY_EP_READ,
										testReadData,
										TEST_READ_LEN,
										out transferred,
										MY_TIMEOUT);
		}
		if (r == (int)MonoUsbError.ErrorTimeout) {
			// This is considered normal operation
			//D.log("Read Timed Out. {0} packet(s) read ({1} bytes)", PacketCount, TransferredTotal);
		} else {
			if (r != 0) {
				// An error, other than ErrorTimeout was received. 
				D.log("Read failed:{0}", (MonoUsbError) r);
			}
			else {
				TransferredTotal += transferred;
				PacketCount++;

				// Display test data.
				D.log("Received {0} bytes", transferred);
				//D.log("Received: ");
				//D.log(System.Text.Encoding.Default.GetString(testReadData, 0, transferred));
			}
		}
	}

	public void OnDisable() {

		// Free and close resources
		if (_deviceHandle != null) {
			if (!_deviceHandle.IsInvalid) {
				MonoUsbApi.ReleaseInterface(_deviceHandle, MY_INTERFACE);
				_deviceHandle.Close();
			}
		}
		if (Session != null) {
			Session.Close();
		}
	}

	// This function originated from do_sync_bulk_transfer()
	// in sync.c of the Libusb-1.0 source code.
	private MonoUsbError DoBulkAsyncTransfer(MonoUsbDeviceHandle devHandle,
													  byte endpoint,
													  byte[] buffer,
													  int length,
													  out int transferred,
													  int timeout) {
		transferred = 0;
		MonoUsbTransfer transfer = new MonoUsbTransfer(0);
		if (transfer.IsInvalid) return MonoUsbError.ErrorNoMem;

		MonoUsbTransferDelegate monoUsbTransferCallbackDelegate = bulkTransferCB;
		int[] userCompleted = new[] { 0 };
		GCHandle gcUserCompleted = GCHandle.Alloc(userCompleted, GCHandleType.Pinned);

		GCHandle gcBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		transfer.FillBulk(
			devHandle,
			endpoint,
			gcBuffer.AddrOfPinnedObject(),
			length,
			monoUsbTransferCallbackDelegate,
			gcUserCompleted.AddrOfPinnedObject(),
			timeout);

		MonoUsbError e = transfer.Submit();
		if ((int)e < 0) {
			transfer.Free();
			gcUserCompleted.Free();
			return e;
		}
		Console.WriteLine("Transfer Submitted..");
		while (userCompleted[0] == 0) {
			int r;
			e = (MonoUsbError)(r = Usb.HandleEvents(Session));
			if (r < 0) {
				if (e == MonoUsbError.ErrorInterrupted)
					continue;
				transfer.Cancel();
				while (userCompleted[0] == 0)
					if (Usb.HandleEvents(Session) < 0)
						break;
				transfer.Free();
				gcUserCompleted.Free();
				return e;
			}
		}

		transferred = transfer.ActualLength;
		e = MonoUsbApi.MonoLibUsbErrorFromTransferStatus(transfer.Status);
		transfer.Free();
		gcUserCompleted.Free();
		return e;
	}

	// This function originated from bulk_transfer_cb()
	// in sync.c of the Libusb-1.0 source code.
	private static void bulkTransferCB(MonoUsbTransfer transfer) {
		Marshal.WriteInt32(transfer.PtrUserData, 1);
		/* caller interprets results and frees transfer */
	}

	// Predicate functions for finding only devices with the specified VendorID & ProductID.
	private bool SpaceNavigatorPredicate(MonoUsbProfile profile) {
		if (profile.DeviceDescriptor.VendorID == SpaceNavigatorVendorID &&
			profile.DeviceDescriptor.ProductID == SpaceNavigatorProductID)
			return true;
		return false;
	}

	public void GetDeviceProfile() {
		_profileList = null;

		// Initialize the context.
		if (Session.IsInvalid)
			throw new Exception("Failed to initialize context.");

		MonoUsbApi.SetDebug(Session, 0);
		// Create a MonoUsbProfileList instance.
		_profileList = new MonoUsbProfileList();

		// The list is initially empty.
		// Each time refresh is called the list contents are updated. 
		int ret = _profileList.Refresh(Session);
		if (ret < 0) throw new Exception("Failed to retrieve device list.");
		D.log("{0} device(s) found.", ret);

		// Iterate through the profile list; write the device descriptor to
		// console output.
		foreach (MonoUsbProfile profile in _profileList.Where(profile => profile.DeviceDescriptor.VendorID == SpaceNavigatorVendorID &&
																		 profile.DeviceDescriptor.ProductID == SpaceNavigatorProductID)) {
			D.log("VendorID {0} productID {1}", profile.DeviceDescriptor.VendorID, profile.DeviceDescriptor.ProductID);
		}

		_profileList.First(SpaceNavigatorPredicate);

		// Since profile list, profiles, and sessions use safe handles the
		// code below is not required but it is considered good programming
		// to explicitly free and close these handle when they are no longer
		// in-use.
		if (_profileList != null)
			_profileList.Close();
	}
}