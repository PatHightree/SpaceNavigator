//==============================================================================

#ifndef _H_connexionclient
#define _H_connexionclient

#include <stdint.h>

//==============================================================================
#ifdef __cplusplus
extern "C" {
#endif
//==============================================================================
#pragma pack(push,2)
//==============================================================================
// Client registration modes

// Use kConnexionClientWildcard ('****') as the application signature in the
// RegisterConnexionClient API to take over the device system-wide in all
// applications:

#define kConnexionClientWildcard		0x2A2A2A2A

// There are two plugin operating modes: one takes over the device
// and disables all built-in driver assignments, the other complements
// the driver by only executing commands that are meant for plugins:

enum {
	kConnexionClientModeTakeOver		= 1,		// take over device completely, driver no longer executes assignments
	kConnexionClientModePlugin			= 2			// receive plugin assignments only, let driver take care of its own
};

//==============================================================================
// Client commands

// The following assignments must be executed by the client:

enum {
	kConnexionCmdNone					= 0,
	kConnexionCmdHandleRawData			= 1,
	kConnexionCmdHandleButtons			= 2,
	kConnexionCmdHandleAxis				= 3,
	
	kConnexionCmdAppSpecific			= 10
};

//==============================================================================
// Messages

// IOServiceOpen and newUserClient client type:

#define kConnexionUserClientType		'3dUC'		// used to create our user client when needed, and the IOHIDDevice one otherwise

// The following messages are sent from the Kernel driver to user space clients:

#define kConnexionMsgDeviceState		'3dSR'		// forwarded device state data
#define kConnexionMsgCalibrateDevice	'3dSC'		// device state data to be used for calibration
#define kConnexionMsgPrefsChanged		'3dPC'		// notify clients that the current app prefs have changed
#define kConnexionMsgDoMapping			'3dDA'		// execute a mapping through the user space helper (should be ignored by clients)
#define kConnexionMsgDoMappingDown		'3dMD'		// down event for mappings that require both down and up events (should be ignored by clients)
#define kConnexionMsgDoMappingUp		'3dMU'		// up event for mappings that require both down and up events (should be ignored by clients)
#define kConnexionMsgDoLongPress		'3dLP'		// forward long press events to the user space helper (should be ignored by clients)
#define kConnexionMsgBatteryStatus		'3dBS'		// forward battery status info to the user space helper (should be ignored by clients)

// Control messages for the kernel driver sent via the ConnexionControl API:

#define kConnexionCtlSetLEDState		'3dsl'		// set the LED state, param = (uint8_t)ledState
#define kConnexionCtlGetDeviceID		'3did'		// get vendorID and productID in the high and low words of the result
#define kConnexionCtlTypeKeystroke		'3dke'		// type a keystroke, param = ((modifiers << 16) | (keyCode << 8) | charCode)
#define kConnexionCtlMoveMouse			'3dmm'		// move the mouse, param = ((deltaX << 16) | deltaY)
#define kConnexionCtlClickButton		'3dcb'		// click a mouse button, param = ((buttonIndex << 16) | buttonState)
#define kConnexionCtlRollWheel			'3dro'		// roll the mouse wheel, param = ((modifiers << 16) | (direction << 8) | amount) - note that modifier keys are NOT released
#define kConnexionCtlReleaseMods		'3dre'		// release modifier keys, param = (modifiers << 16)
#define kConnexionCtlCalibrate			'3dca'		// calibrate the device with the current axes values (same as executing the calibrate assignment)
#define kConnexionCtlUncalibrate		'3dde'		// uncalibrate the device (i.e. reset calibration to 0,0,0,0,0,0)
#define kConnexionCtlDoMapping			'3ddm'		// perform a mapping, param = ((mappingIndex << 16) | mappingState)

#define kConnexionCtlOpenPrefPane		'3dop'		// open the 3dconnexion preference pane in System Preferences
#define kConnexionCtlSetSwitches		'3dss'		// set the current state of the client-controlled feature switches (bitmap, see masks below)
#define kConnexionCtlPopupMenuRunning	'3dme'		// inform that system-wide popup menu is being displayed, param = (uint8_t)menuCurrentlyRunning

// Client capability mask constants (this mask defines which buttons and controls should be sent to clients, the others are handled by the driver)

#define kConnexionMaskButton1			0x0001
#define kConnexionMaskButton2			0x0002
#define kConnexionMaskButton3			0x0004
#define kConnexionMaskButton4			0x0008
#define kConnexionMaskButton5			0x0010
#define kConnexionMaskButton6			0x0020
#define kConnexionMaskButton7			0x0040
#define kConnexionMaskButton8			0x0080

#define kConnexionMaskAxis1				0x0100
#define kConnexionMaskAxis2				0x0200
#define kConnexionMaskAxis3				0x0400
#define kConnexionMaskAxis4				0x0800
#define kConnexionMaskAxis5				0x1000
#define kConnexionMaskAxis6				0x2000

#define kConnexionMaskButtons			0x00FF		// note: this only specifies the first 8 buttons, kept for backwards compatibility
#define kConnexionMaskAxisTrans			0x0700
#define kConnexionMaskAxisRot			0x3800
#define kConnexionMaskAxis				0x3F00
#define kConnexionMaskAll				0x3FFF

// Added in version 10:0 to support all 32 buttons on the SpacePilot Pro, use with the new SetConnexionClientButtonMask API

#define kConnexionMaskButton9			0x00000100
#define kConnexionMaskButton10			0x00000200
#define kConnexionMaskButton11			0x00000400
#define kConnexionMaskButton12			0x00000800
#define kConnexionMaskButton13			0x00001000
#define kConnexionMaskButton14			0x00002000
#define kConnexionMaskButton15			0x00004000
#define kConnexionMaskButton16			0x00008000

#define kConnexionMaskButton17			0x00010000
#define kConnexionMaskButton18			0x00020000
#define kConnexionMaskButton19			0x00040000
#define kConnexionMaskButton20			0x00080000
#define kConnexionMaskButton21			0x00100000
#define kConnexionMaskButton22			0x00200000
#define kConnexionMaskButton23			0x00400000
#define kConnexionMaskButton24			0x00800000

#define kConnexionMaskButton25			0x01000000
#define kConnexionMaskButton26			0x02000000
#define kConnexionMaskButton27			0x04000000
#define kConnexionMaskButton28			0x08000000
#define kConnexionMaskButton29			0x10000000
#define kConnexionMaskButton30			0x20000000
#define kConnexionMaskButton31			0x40000000
#define kConnexionMaskButton32			0x80000000

#define kConnexionMaskAllButtons		0xFFFFFFFF

// Masks for client-controlled feature switches

#define kConnexionSwitchZoomOnY			0x0001
#define kConnexionSwitchDominant		0x0002
#define kConnexionSwitchEnableAxis1		0x0004
#define kConnexionSwitchEnableAxis2		0x0008
#define kConnexionSwitchEnableAxis3		0x0010
#define kConnexionSwitchEnableAxis4		0x0020
#define kConnexionSwitchEnableAxis5		0x0040
#define kConnexionSwitchEnableAxis6		0x0080
#define kConnexionSwitchReverseAxis1	0x0100
#define kConnexionSwitchReverseAxis2	0x0200
#define kConnexionSwitchReverseAxis3	0x0400
#define kConnexionSwitchReverseAxis4	0x0800
#define kConnexionSwitchReverseAxis5	0x1000
#define kConnexionSwitchReverseAxis6	0x2000

#define kConnexionSwitchEnableTrans		0x001C
#define kConnexionSwitchEnableRot		0x00E0
#define kConnexionSwitchEnableAll		0x00FC
#define kConnexionSwitchReverseTrans	0x0700
#define kConnexionSwitchReverseRot		0x3800
#define kConnexionSwitchReverseAll		0x3F00

#define kConnexionSwitchesDisabled		0x80000000	// use driver defaults instead of client-controlled switches

//==============================================================================
// Device state record

// Structure type and current version:

#define kConnexionDeviceStateType		0x4D53		// 'MS' (Connexion State)
#define kConnexionDeviceStateVers		0x6D33		// 'm3' (version 3 includes 32-bit button data in previously unused field, binary compatible with version 2)

// This structure is used to forward device data and commands from the kext to the client:

typedef struct {
// header
	uint16_t		version;							// kConnexionDeviceStateVers
	uint16_t		client;								// identifier of the target client when sending a state message to all user clients
// command
	uint16_t		command;							// command for the user-space client
	int16_t			param;								// optional parameter for the specified command
	int32_t			value;								// optional value for the specified command
	UInt64			time;								// timestamp for this message (clock_get_uptime)
// raw report
	uint8_t			report[8];							// raw USB report from the device
// processed data
	uint16_t		buttons8;							// buttons (first 8 buttons only, for backwards binary compatibility- use "buttons" field instead)
	int16_t			axis[6];							// x, y, z, rx, ry, rz
// reserved for future use
	uint16_t		address;							// USB device address, used to tell one device from the other
	uint32_t		buttons;							// buttons
} ConnexionDeviceState, *ConnexionDeviceStatePtr;

// Size of the above structure:

#define kConnexionDeviceStateSize (sizeof(ConnexionDeviceState))

//==============================================================================
// Device IDs for 3Dconnexion devices with separate and different preferences.
// NOTE: These IDs are no longer internally used by the driver, and the
// ConnexionGetCurrentDevicePrefs API always returns kDevID_AnyDevice in the
// deviceID field. The definitions are kept here for backwards compatibility only.

#define kDevID_SpaceNavigator			0x00		// PID 0xC626
#define kDevID_SpaceNavigatorNB			0x01		// PID 0xC628
#define kDevID_SpaceExplorer			0x02		// PID 0xC627
#define kDevID_SpaceTraveler			0x03		// PID 0xC623
#define kDevID_SpacePilot				0x04		// PID 0xC625
#define kDevID_SpacePilotPro			0x05		// PID 0xC629
#define kDevID_SpaceMousePro			0x06		// PID 0xC62B
#define kDevID_Count					7			// number of device IDs
#define kDevID_AnyDevice				0x7FFF		// widcard used to specify any available device

//==============================================================================
// Device prefs record

// Structure type and current version:

#define kConnexionDevicePrefsType		0x4D50		// 'MP' (Connexion Prefs)
#define kConnexionDevicePrefsVers		0x7031		// 'p1' (version 1)

// This structure is used to retrieve the current device prefs from the helper:

typedef struct {
// header
	uint16_t				type;					// kConnexionDevicePrefsType
	uint16_t				version;				// kConnexionDevicePrefsVers
	uint16_t				deviceID;				// device ID (SpaceNavigator, SpaceNavigatorNB, SpaceExplorer...)
	uint16_t				reserved1;				// set to 0
// target application
	uint32_t				appSignature;			// target application signature
	uint32_t				reserved2;				// set to 0
	uint8_t					appName[64];			// target application name (Pascal string with length byte at the beginning)
// device preferences
	uint8_t					mainSpeed;				// overall speed
	uint8_t					zoomOnY;				// use Y axis for zoom, Z axis for un/down pan
	uint8_t					dominant;				// only respond to the largest one of all 6 axes values at any given time
	uint8_t					reserved3;				// set to 0
	int8_t					mapV[6];				// axes mapping when Zoom direction is on vertical axis (zoomOnY = 0)
	int8_t					mapH[6];				// axes mapping when Zoom direction is on horizontal axis (zoomOnY != 0)
	uint8_t					enabled[6];				// enable or disable individual axes
	uint8_t					reversed[6];			// reverse individual axes
	uint8_t					speed[6];				// speed for individual axes (min 0, max 200, reserved 201-255)
	uint8_t					sensitivity[6];			// sensitivity for individual axes (min 0, max 200, reserved 201-255)
	int32_t					scale[6];				// 10000 * scale and "natural" reverse state for individual axes
// added in version 10.0 (build 136)
	uint32_t				gamma;					// 1000 * gamma value used to compute nonlinear axis response, use 1000 (1.0) for linear response
	uint32_t				intersect;				// intersect value used for gamma computations
} ConnexionDevicePrefs, *ConnexionDevicePrefsPtr;

// Size of the above structure:

#define kConnexionDevicePrefsSize (sizeof(ConnexionDevicePrefs))

//==============================================================================
#pragma pack(pop)
//==============================================================================
#ifdef __cplusplus
}
#endif
//==============================================================================

#endif	// _H_connexionclient

//==============================================================================
