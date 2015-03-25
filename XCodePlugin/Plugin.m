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

MonoDomain *domain;
NSString *assemblyPath;
MonoAssembly *monoAssembly;
MonoImage *monoImage;

MonoMethodDesc *trackPadTouchBeganDesc;
MonoMethod *trackPadTouchBeganMethod;

MonoMethodDesc *trackPadTouchEndedDesc;
MonoMethod *trackPadTouchEndedMethod;

MonoMethodDesc *trackPadTouchCanceledDesc;
MonoMethod *trackPadTouchCanceledMethod;

NSView* view = nil;

@interface TrackingObject : NSResponder
{
}
- (void)touchesBeganWithEvent:(NSEvent *)event;
- (void)touchesEndedWithEvent:(NSEvent *)event;
- (void)touchesCancelledWithEvent:(NSEvent *)event;

@end

@implementation TrackingObject

- (void)touchesBeganWithEvent:(NSEvent *)event
{
	float posX = 0.0f;
	float posY = 0.0f;

	NSSet *touches = [event touchesMatchingPhase:NSTouchPhaseBegan inView:view];
	for (NSTouch *touch in touches)
	{
		posX = touch.normalizedPosition.x;
		posY = touch.normalizedPosition.y;
		break; //We just track one touch (no multi-touch in this example)
	}

	//Call method back
	void *args[] = { &posX, &posY };
	mono_runtime_invoke(trackPadTouchBeganMethod, NULL, args, NULL);
}

- (void)touchesEndedWithEvent:(NSEvent *)event
{
	float posX = 0.0f;
	float posY = 0.0f;

	NSSet *touches = [event touchesMatchingPhase:NSTouchPhaseBegan inView:view];
	for (NSTouch *touch in touches)
	{
		posX = touch.normalizedPosition.x;
		posY = touch.normalizedPosition.y;
		break;
	}

	//Call method back
	void *args[] = { &posX, &posY };
	mono_runtime_invoke(trackPadTouchEndedMethod, NULL, args, NULL);
}

- (void)touchesCancelledWithEvent:(NSEvent *)event
{
	float posX = 0.0f;
	float posY = 0.0f;

	NSSet *touches = [event touchesMatchingPhase:NSTouchPhaseBegan inView:view];
	for (NSTouch *touch in touches)
	{
		posX = touch.normalizedPosition.x;
		posY = touch.normalizedPosition.y;
		break;
	}

	//Call method back
	void *args[] = { &posX, &posY };
	mono_runtime_invoke(trackPadTouchCanceledMethod, NULL, args, NULL);
}

@end

TrackingObject* pTrackMgr = nil;


void SetupTrackingObject()
{
    NSLog(@"Native plugin -> SetupTrackingObject");
    
//    NSApplication* app = [NSApplication sharedApplication];
//    NSWindow* window = [app mainWindow];
//    view = [window contentView];
//    
//    if(pTrackMgr != nil)
//    {
//        [pTrackMgr release];
//        pTrackMgr = nil;
//    }
//    
//    pTrackMgr = [TrackingObject alloc];
//    [view setAcceptsTouchEvents:YES];
//    [view setNextResponder:pTrackMgr];
    
}

void InitPlugin(bool isDevice)
{
	NSLog(@"Native plugin -> PluginInit - device? : %d", isDevice);

//	SetupTrackingObject();
//
//	//We do this so it also works while we work on the Editor
//	if(isDevice)
//	{
//		assemblyPath = [[[NSBundle mainBundle] bundlePath]
//						stringByAppendingPathComponent:@"Contents/Data/Managed/Assembly-CSharp.dll"];
//	}
//	else
//	{
//		assemblyPath = @"[PATH_TO_YOUR_PROJECT]/Library/ScriptAssemblies/Assembly-CSharp.dll";
//	}
//
//	NSLog(@"Native plugin -> assembly path: %@", assemblyPath);
//
//	domain = mono_domain_get();
//	monoAssembly = mono_domain_assembly_open(domain, assemblyPath.UTF8String);
//	monoImage = mono_assembly_get_image(monoAssembly);
//
//	//Note here that "MyNativePlugin" is the name of the unity class and "TrackPadTouchBegan" is the name of the static method we defined before.
//	trackPadTouchBeganDesc = mono_method_desc_new("MyNativePlugin:TrackPadTouchBegan", FALSE);
//	trackPadTouchBeganMethod = mono_method_desc_search_in_image(trackPadTouchBeganDesc, monoImage);
//	mono_method_desc_free(trackPadTouchBeganDesc);
//
//	trackPadTouchEndedDesc = mono_method_desc_new("MyNativePlugin:TrackPadTouchEnded", FALSE);
//	trackPadTouchEndedMethod = mono_method_desc_search_in_image(trackPadTouchEndedDesc, monoImage);
//	mono_method_desc_free(trackPadTouchEndedDesc);
//
//	trackPadTouchCanceledDesc = mono_method_desc_new("MyNativePlugin:TrackPadTouchCanceled", FALSE);
//	trackPadTouchCanceledMethod = mono_method_desc_search_in_image(trackPadTouchCanceledDesc, monoImage);
//	mono_method_desc_free(trackPadTouchCanceledDesc);

}











































//==============================================================================
// Make the linker happy for the framework check (see link below for more info)
// http://developer.apple.com/documentation/MacOSX/Conceptual/BPFrameworks/Concepts/WeakLinking.html

extern OSErr InstallConnexionHandlers() __attribute__((weak_import));

//==============================================================================
// Quick & dirty way to access our class variables from the C callback

ConnexionTest	*gConnexionTest = 0L;

//==============================================================================
@implementation ConnexionTest
//==============================================================================

- (void) awakeFromNib
{
    OSErr	error;
    
    // Quick hack to keep the sample as simple as possible, don't use in shipping code
    gConnexionTest = self;
    
    // Make sure the framework is installed
    if(InstallConnexionHandlers != NULL)
    {
        // Install message handler and register our client
        error = InstallConnexionHandlers(TestMessageHandler, 0L, 0L);
        
        // This takes over in our application only
        fConnexionClientID = RegisterConnexionClient('MCTt', "\p3DxClientTest", kConnexionClientModeTakeOver, kConnexionMaskAll);
        
        // This takes over system-wide
        // fConnexionClientID = RegisterConnexionClient(kConnexionClientWildcard, 0L, kConnexionClientModeTakeOver, kConnexionMaskAll);
        
        // Remove warning message about the framework not being available
        //[mtFWNotFound removeFromSuperview];
    }
}

//==============================================================================

- (void) windowWillClose: (NSNotification*)notification
{
    printf("3DxClientTest windowWillClose - unregistering client\n");
    
    // Make sure the framework is installed
    if(InstallConnexionHandlers != NULL)
    {
        // Unregister our client and clean up all handlers
        if(fConnexionClientID) UnregisterConnexionClient(fConnexionClientID);
        CleanupConnexionHandlers();
    }
}

//==============================================================================

void TestMessageHandler(io_connect_t connection, natural_t messageType, void *messageArgument)
{
    static ConnexionDeviceState	lastState;
    ConnexionDeviceState		*state;
    
    switch(messageType)
    {
        case kConnexionMsgDeviceState:
            state = (ConnexionDeviceState*)messageArgument;
            if(state->client == gConnexionTest->fConnexionClientID)
            {
                // decipher what command/event is being reported by the driver
                switch (state->command)
                {
                    case kConnexionCmdHandleAxis:
                        if(state->axis[0] != lastState.axis[0])	[gConnexionTest->mtValueX		setStringValue:[NSString stringWithFormat:@"%d", (int)state->axis[0]]];
                        if(state->axis[1] != lastState.axis[1])	[gConnexionTest->mtValueY		setStringValue:[NSString stringWithFormat:@"%d", (int)state->axis[1]]];
                        if(state->axis[2] != lastState.axis[2])	[gConnexionTest->mtValueZ		setStringValue:[NSString stringWithFormat:@"%d", (int)state->axis[2]]];
                        if(state->axis[3] != lastState.axis[3])	[gConnexionTest->mtValueRx		setStringValue:[NSString stringWithFormat:@"%d", (int)state->axis[3]]];
                        if(state->axis[4] != lastState.axis[4])	[gConnexionTest->mtValueRy		setStringValue:[NSString stringWithFormat:@"%d", (int)state->axis[4]]];
                        if(state->axis[5] != lastState.axis[5])	[gConnexionTest->mtValueRz		setStringValue:[NSString stringWithFormat:@"%d", (int)state->axis[5]]];
                        break;
                        
                    case kConnexionCmdHandleButtons:
                        if(state->buttons != lastState.buttons)	[gConnexionTest->mtValueButtons	setStringValue:[NSString stringWithFormat:@"%d", (int)state->buttons]];
                        break;
                }
                memmove(state, &lastState, (long)sizeof(ConnexionDeviceState));
                //BlockMoveData(state, &lastState, (long)sizeof(ConnexionDeviceState));
            }
            break;
            
        default:
            // other messageTypes can happen and should be ignored
            break;
    }
}

//==============================================================================
@end
//==============================================================================s
