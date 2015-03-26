//
//  Plugin.m
//  XCodePlugin
//
//  Created by Patrick Hogenboom on 15/03/15.
//  Copyright (c) 2015 Patrick Hogenboom. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>
#import "Plugin.h"
#import "XCodePlugin_Prefix.pch"
#import <3DConnexionClient/ConnexionClientAPI.h>

ConnexionListener	*gConnexionListener = 0L;

//==============================================================================
// Public API

int InitDevice()
{
    gConnexionListener = [[ConnexionListener alloc] init];
    return [gConnexionListener initDevice];
}

void SampleDevice(int* x, int* y, int* z, int* rx, int* ry, int* rz)
{
    *x = gConnexionListener->mtValueX;
    *y = gConnexionListener->mtValueY;
    *z = gConnexionListener->mtValueZ;
    *rx = gConnexionListener->mtValueRx;
    *ry = gConnexionListener->mtValueRy;
    *rz = gConnexionListener->mtValueRz;
}

int DisposeDevice()
{
    return [gConnexionListener disposeDevice];
}


//==============================================================================
// Make the linker happy for the framework check (see link below for more info)
// http://developer.apple.com/documentation/MacOSX/Conceptual/BPFrameworks/Concepts/WeakLinking.html

extern OSErr InstallConnexionHandlers() __attribute__((weak_import));

//==============================================================================
@implementation ConnexionListener
//==============================================================================

- (OSErr) initDevice
{
    OSErr	error;
    
    // Make sure the framework is installed
    if(InstallConnexionHandlers != NULL)
    {
        // Install message handler and register our client
        error = InstallConnexionHandlers(MessageHandler, 0L, 0L);
        
        // This takes over in our application only
        // fConnexionClientID = RegisterConnexionClient('MCTt', NULL, kConnexionClientModeTakeOver, kConnexionMaskAll);
        
        // This takes over system-wide
        fConnexionClientID = RegisterConnexionClient(kConnexionClientWildcard, 0L, kConnexionClientModeTakeOver, kConnexionMaskAll);
        
        // Remove warning message about the framework not being available
        //[mtFWNotFound removeFromSuperview];
        if (error >= 0)
            error = fConnexionClientID;
    }
    else
        error = -1;
    return error;
}

//==============================================================================

- (int) disposeDevice
{
    // Make sure the framework is installed
    if(InstallConnexionHandlers != NULL)
    {
        // Unregister our client and clean up all handlers
        if(fConnexionClientID) UnregisterConnexionClient(fConnexionClientID);
            CleanupConnexionHandlers();
    }
    return fConnexionClientID;
}

//==============================================================================

void MessageHandler(io_connect_t connection, natural_t messageType, void *messageArgument)
{
    static ConnexionDeviceState	lastState;
    ConnexionDeviceState		*state;
    
    switch(messageType)
    {
        case kConnexionMsgDeviceState:

            state = (ConnexionDeviceState*)messageArgument;
            if(state->client == gConnexionListener->fConnexionClientID)
            {
                // decipher what command/event is being reported by the driver
                switch (state->command)
                {
                    case kConnexionCmdHandleAxis:
                        if(state->axis[0] != lastState.axis[0]) gConnexionListener->mtValueX = (int)state->axis[0];
                        if(state->axis[1] != lastState.axis[1])	gConnexionListener->mtValueY = (int)state->axis[1];
                        if(state->axis[2] != lastState.axis[2])	gConnexionListener->mtValueZ = (int)state->axis[2];
                        if(state->axis[3] != lastState.axis[3])	gConnexionListener->mtValueRx = (int)state->axis[3];
                        if(state->axis[4] != lastState.axis[4])	gConnexionListener->mtValueRy = (int)state->axis[4];
                        if(state->axis[5] != lastState.axis[5])	gConnexionListener->mtValueRz = (int)state->axis[5];
                        break;
                        
                    case kConnexionCmdHandleButtons:
                        if(state->buttons != lastState.buttons)	[gConnexionListener->mtValueButtons	setStringValue:[NSString stringWithFormat:@"%d", (int)state->buttons]];
                        break;
                }
                memmove(state, &lastState, (long)sizeof(ConnexionDeviceState));
            }
            break;
            
        default:
            // Reset input data
            gConnexionListener->mtValueX = 0;
            gConnexionListener->mtValueY = 0;
            gConnexionListener->mtValueZ = 0;
            gConnexionListener->mtValueRx = 0;
            gConnexionListener->mtValueRy = 0;
            gConnexionListener->mtValueRz = 0;
            break;
    }
}

//==============================================================================
@end
//==============================================================================s
