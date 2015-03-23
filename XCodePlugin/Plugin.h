//
//  Plugin.h
//  XCodePlugin
//
//  Created by Ewoud Wijma on 15/03/15.
//  Copyright (c) 2015 Patrick Hogenboom. All rights reserved.
//

//==============================================================================
@interface ConnexionTest : NSObject
//==============================================================================
{
    IBOutlet id		mtMainWindow;
    IBOutlet id		mtValueButtons;
    IBOutlet id		mtValueX;
    IBOutlet id		mtValueY;
    IBOutlet id		mtValueZ;
    IBOutlet id		mtValueRx;
    IBOutlet id		mtValueRy;
    IBOutlet id		mtValueRz;
    IBOutlet id		mtFWNotFound;
    
    UInt16			fConnexionClientID;
}
//==============================================================================

- (void)	awakeFromNib;
- (void)	windowWillClose:		(NSNotification*)notification;

void TestMessageHandler(io_connect_t connection, natural_t messageType, void *messageArgument);

//==============================================================================
@end
//==============================================================================
