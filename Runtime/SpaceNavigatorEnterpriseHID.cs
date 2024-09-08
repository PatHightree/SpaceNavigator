using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceNavigatorDriver
{
    struct SpaceNavigatorEnterpriseHIDState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('H', 'I', 'D');

        public struct ReportFormat1
        {
            public Vector3 translation;
        }

        public struct ReportFormat2
        {
            public Vector3 rotation;
        }

        public struct ReportFormat3
        {
            public byte buttons;
        }

        // 1st report
        [InputControl(name = "translation", format = "VC3S", layout = "Vector3", displayName = "Translation")] 
        [InputControl(name = "translation/x", offset = 0, format = "SHRT", parameters = "scale=true, scaleFactor=10")] 
        [InputControl(name = "translation/y", offset = 4, format = "SHRT", parameters = "scale=true, scaleFactor=-10")]
        [InputControl(name = "translation/z", offset = 2, format = "SHRT", parameters = "scale=true, scaleFactor=-10")]

        [InputControl(name = "rotation", format = "VC3S", layout = "Vector3", displayName = "Rotation")] 
        [InputControl(name = "rotation/x", offset = 7, format = "SHRT", parameters = "scale=true, scaleFactor=-10")] 
        [InputControl(name = "rotation/y", offset = 9, format = "SHRT", parameters = "scale=true, scaleFactor=10")] 
        [InputControl(name = "rotation/z", offset = 11, format = "SHRT", parameters = "scale=true, scaleFactor=10")]

        public ReportFormat1 report1;

        // 2nd report
        public ReportFormat2 report2;
        
        // 3rd report
        [InputControl(name = "button1", bit = 0, format = "BIT", layout = "Button", displayName = "Button 1")] 
        [InputControl(name = "button2", bit = 1, format = "BIT", layout = "Button", displayName = "Button 2")]
        public ReportFormat3 report3;
    }

#if UNITY_EDITOR
    [InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
    [InputControlLayout(stateType = typeof(SpaceNavigatorEnterpriseHIDState))]
    public class SpaceNavigatorEnterpriseHID : SpaceNavigatorHID
    {
        static SpaceNavigatorEnterpriseHID()
        {
            // Register a layout with product ID, so this layout will have a higher score than SpaceNavigatorHID
            InputSystem.RegisterLayout<SpaceNavigatorEnterpriseHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("3Dconnexion.*")
                    .WithCapability("productId", 0xC633));
            DebugLog("SpaceNavigatorEnterpriseHID : Register layout for SpaceNavigator Enterprise productId:0x????");
        }

        // When one of our custom devices is removed, we want to make sure that if
        // it is the '.current' device, we null out '.current'.
        public override unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            // Refuse delta events.
            if (eventPtr.IsA<DeltaStateEvent>())
                return;

            var stateEventPtr = StateEvent.From(eventPtr);
            if (stateEventPtr->stateFormat != new FourCC('H', 'I', 'D'))
                return;

            var reportPtr = (byte*) stateEventPtr->state;
            var reportId = *reportPtr;
            var reportStatePtr = (reportPtr + 1); // or wherever the actual report starts.

            // We have two options here. We can either use InputState.Change with a DeltaStateEvent that we set up
            // from the event we have received (and simply update either report1 or report2 only) or we can merge
            // our current state with the state we have just received. The latter is simpler so we do that here.

            var newState = default(SpaceNavigatorEnterpriseHIDState);
            // Can opt to only copy the state that we won't override. We don't bother here.
            UnsafeUtility.MemCpy(&newState, (byte*) currentStatePtr + stateBlock.byteOffset, sizeof(SpaceNavigatorEnterpriseHIDState));

            switch (reportId)
            {
                case 1:
                    UnsafeUtility.MemCpy(&newState.report1, reportStatePtr, sizeof(SpaceNavigatorEnterpriseHIDState.ReportFormat1));
                    DebugLog("SpaceNavigatorEnterpriseHID : Copied report1");
                    break;
                case 3:
                    UnsafeUtility.MemCpy(&newState.report3, reportStatePtr, sizeof(SpaceNavigatorEnterpriseHIDState.ReportFormat3));
                    DebugLog("SpaceNavigatorEnterpriseHID : Copied report3");
                    break;
            }

            // Apply the state change. Don't simply MemCpy over currentStatePtr as that will lead to various
            // malfunctions. The system needs to do the memcpy itself.
            InputState.Change(this, newState, eventPtr: eventPtr);
        }
    }
}