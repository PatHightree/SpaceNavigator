//==============================================================================

#ifndef _H_connexionclientapi
#define _H_connexionclientapi

#include <IOKit/IOKitLib.h>

#include "ConnexionClient.h"

//==============================================================================
#ifdef __cplusplus
extern "C" {
#endif
//==============================================================================
// Callback procedure types

typedef void	(*ConnexionAddedHandlerProc)		(io_connect_t connection);
typedef void	(*ConnexionRemovedHandlerProc)		(io_connect_t connection);
typedef void	(*ConnexionMessageHandlerProc)		(io_connect_t connection, natural_t messageType, void *messageArgument);

// NOTE for ConnexionMessageHandlerProc:
// when messageType == kConnexionMsgDeviceState, messageArgument points to ConnexionDeviceState with size kConnexionDeviceStateSize
// when messageType == kConnexionMsgPrefsChanged, messageArgument points to the target application signature with size sizeof(UInt32)

//==============================================================================
// Public APIs to be called once when the application starts up or shuts down

OSErr			InstallConnexionHandlers			(ConnexionMessageHandlerProc messageHandler, ConnexionAddedHandlerProc addedHandler, ConnexionRemovedHandlerProc removedHandler);
void			CleanupConnexionHandlers			(void);

//==============================================================================
// Public APIs to be called whenever the app wants to start/stop receiving data
// the mask parameter (client capabilities mask) specifies which controls must be forwarded to the client
// buttonMask (previously part of the client capabilities mask) specifies which buttons must be forwarded to the client

UInt16			RegisterConnexionClient				(UInt32 signature, UInt8 *name, UInt16 mode, UInt32 mask);
void			SetConnexionClientMask				(UInt16 clientID, UInt32 mask);
void			SetConnexionClientButtonMask		(UInt16 clientID, UInt32 buttonMask);
void			UnregisterConnexionClient			(UInt16 clientID);

//==============================================================================
// Public API to send control commands to the driver and retrieve a result value
// Note: the new ConnexionClientControl variant is strictly required for
// kConnexionCtlSetSwitches and kConnexionCtlClearSwitches but also works for
// all other Control calls. The old variant remains for backwards compatibility.

OSErr			ConnexionControl					(UInt32 message, SInt32 param, SInt32 *result);
OSErr			ConnexionClientControl				(UInt16 clientID, UInt32 message, SInt32 param, SInt32 *result);

//==============================================================================
// Public API to fetch the current device preferences for either the first connected device or a specific device type (kDevID_Xxx)

OSErr			ConnexionGetCurrentDevicePrefs		(UInt32 deviceID, ConnexionDevicePrefs *prefs);

//==============================================================================
// Public API to set all button labels in the iOS/Android "virtual device" apps

OSErr			ConnexionSetButtonLabels			(UInt8 *labels, UInt16 size);

// Labels data is a series of 32 variable-length null-terminated UTF8-encoded strings.
// The sequence of strings follows the SpacePilot Pro button numbering.
// Empty strings revert the button label to its default value.
// As an example, this data would set the label for button Top to "Top" and
// revert all other button labels to their default values:
// 
//	0x00,						// empty string for Menu
//	0x00,						// empty string for Fit
//	0x54, 0x6F, 0x70, 0x00,		// utf-8 encoded "Top" string for Top
//	0x00,						// empty string for Left
//	0x00, 0x00, 0x00, 0x00,		// empty strings for Right, Front, etc...
//	0x00, 0x00, 0x00, 0x00,
//	0x00, 0x00, 0x00, 0x00,
//	0x00, 0x00, 0x00, 0x00,
//	0x00, 0x00, 0x00, 0x00,
//	0x00, 0x00, 0x00, 0x00,
//	0x00, 0x00, 0x00, 0x00
//==============================================================================
#ifdef __cplusplus
}
#endif
//==============================================================================

#endif	// _H_connexionclientapi

//==============================================================================
