//
//  3DConnexionWrapperU4.h
//  3DConnexionWrapper
//
//  Created by Ewoud Wijma on 30/03/15.
//  Copyright (c) 2015 Patrick Hogenboom. All rights reserved.
//

#ifndef _DConnexionWrapper__DConnexionWrapperU4_h
#define _DConnexionWrapper__DConnexionWrapperU4_h

//==============================================================================
@interface ConnexionListener : NSObject
//==============================================================================
{
    IBOutlet id		mtMainWindow;
    IBOutlet id		mtValueButtons;
@public int		mtValueX;
@public int		mtValueY;
@public int		mtValueZ;
@public int		mtValueRx;
@public int		mtValueRy;
@public int		mtValueRz;
    IBOutlet id		mtFWNotFound;
@public int     mDebug;
    
    UInt16			fConnexionClientID;
}
//==============================================================================

- (OSErr)	initDevice;
- (int)     disposeDevice;

void MessageHandler(io_connect_t connection, natural_t messageType, void *messageArgument);

//==============================================================================
@end
//==============================================================================

#endif
