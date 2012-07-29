#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

using System;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using UnityEngine;

public class LibUsbDotNetExperiment : MonoBehaviour {
	public UsbDevice MyUsbDevice;

	private const int SpaceNavigatorVendorID = 0x046d;
	private const int SpaceNavigatorProductID = 0xc626;
	public UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(SpaceNavigatorVendorID, SpaceNavigatorProductID);

	void Awake() {
		ErrorCode ec = ErrorCode.None;

		try {
			// Find and open the usb device.
			MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

			// If the device is open and ready
			if (MyUsbDevice == null) throw new Exception("Device Not Found.");

			// If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
			// it exposes an IUsbDevice interface. If not (WinUSB) the 
			// 'wholeUsbDevice' variable will be null indicating this is 
			// an interface of a device; it does not require or support 
			// configuration and interface selection.
			IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
			if (!ReferenceEquals(wholeUsbDevice, null)) {
				// This is a "whole" USB device. Before it can be used, 
				// the desired configuration and interface must be selected.

				// Select config #1
				wholeUsbDevice.SetConfiguration(1);

				// Claim interface #0.
				wholeUsbDevice.ClaimInterface(0);
			}

			// open read endpoint 1.
			UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);


			byte[] readBuffer = new byte[1024];
			while (ec == ErrorCode.None) {
				int bytesRead;

				// If the device hasn't sent data in the last 5 seconds,
				// a timeout error (ec = IoTimedOut) will occur. 
				ec = reader.Read(readBuffer, 5000, out bytesRead);

				if (bytesRead == 0) throw new Exception(string.Format("{0}:No more bytes!", ec));
				D.log("{0} bytes read", bytesRead);

				// Write that output to the console.
				D.log(Encoding.Default.GetString(readBuffer, 0, bytesRead));
			}

			D.log("\r\nDone!\r\n");
		}
		catch (Exception ex) {
			D.log((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
		} finally {
			if (MyUsbDevice != null) {
				if (MyUsbDevice.IsOpen) {
					// If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
					// it exposes an IUsbDevice interface. If not (WinUSB) the 
					// 'wholeUsbDevice' variable will be null indicating this is 
					// an interface of a device; it does not require or support 
					// configuration and interface selection.
					IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
					if (!ReferenceEquals(wholeUsbDevice, null)) {
						// Release interface #0.
						wholeUsbDevice.ReleaseInterface(0);
					}

					MyUsbDevice.Close();
				}
				MyUsbDevice = null;

				// Free usb resources
				UsbDevice.Exit();

			}

			// Wait for user input..
			//Console.ReadKey();
		}
	}

	void Start() {
	}

	void Update() {
	}
}
