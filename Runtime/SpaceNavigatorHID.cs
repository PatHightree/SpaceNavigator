using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;

namespace SpaceNavigatorDriver
{

    [StructLayout(LayoutKind.Explicit)]
    struct SpaceNavigatorHIDState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('H', 'I', 'D');

        [FieldOffset(0)]
        public byte reportId;

        // 1st report
        // Normalize min/max values: Incoming values are in the range of [-350, 350], so we just use 350/Int16.MaxValue ~= 0.0106
        [InputControl(name = "translation", format = "VC3S", layout = "Vector3", displayName = "Translation")]
        [InputControl(name = "translation/x", offset = 0, format = "SHRT", parameters = "normalize=true,normalizeMin=-0.0106,normalizeMax=0.0106,normalizeZero=0.0, clamp=2,clampMin=-1,clampMax=1")]
        [InputControl(name = "translation/y", offset = 4, format = "SHRT", parameters = "normalize=true,normalizeMin=-0.0106,normalizeMax=0.0106,normalizeZero=0.0, clamp=2,clampMin=-1,clampMax=1, invert=true")]
        [InputControl(name = "translation/z", offset = 2, format = "SHRT", parameters = "normalize=true,normalizeMin=-0.0106,normalizeMax=0.0106,normalizeZero=0.0, clamp=2,clampMin=-1,clampMax=1, invert=true")]
        [FieldOffset(1)]
        public short translationX;
        [FieldOffset(3)]
        public short translationY;
        [FieldOffset(5)]
        public short translationZ;

        [InputControl(name = "rotation", format = "VC3S", layout = "Vector3", displayName = "Rotation")]
        [InputControl(name = "rotation/x", offset = 0, format = "SHRT", parameters = "normalize=true,normalizeMin=-0.0106,normalizeMax=0.0106,normalizeZero=0.0, clamp=2,clampMin=-1,clampMax=1, invert=true")]
        [InputControl(name = "rotation/y", offset = 4, format = "SHRT", parameters = "normalize=true,normalizeMin=-0.0106,normalizeMax=0.0106,normalizeZero=0.0, clamp=2,clampMin=-1,clampMax=1")]
        [InputControl(name = "rotation/z", offset = 2, format = "SHRT", parameters = "normalize=true,normalizeMin=-0.0106,normalizeMax=0.0106,normalizeZero=0.0, clamp=2,clampMin=-1,clampMax=1")]
        [FieldOffset(7)]
        public short rotationX;
        [FieldOffset(9)]
        public short rotationY;
        [FieldOffset(11)]
        public short rotationZ;

        // 3rd report
        [InputControl(name = "button1", bit = 0, format = "BIT", layout = "Button", displayName = "Button 1")]
        [InputControl(name = "button2", bit = 1, format = "BIT", layout = "Button", displayName = "Button 2")]
        [FieldOffset(13)]
        public ButtonReport buttonsState;

        [StructLayout(LayoutKind.Explicit)]
        public struct ButtonReport
        {
            [FieldOffset(0)]
            public byte reportId;

            [FieldOffset(1)]
            public bool button1;

            [FieldOffset(2)]
            public bool button2;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
    [InputControlLayout(stateType = typeof(SpaceNavigatorHIDState))]
    public class SpaceNavigatorHID : InputDevice, IInputStateCallbackReceiver
    {
        public ButtonControl Button1 { get; protected set; }
        public ButtonControl Button2 { get; protected set; }
        public Vector3Control Rotation { get; protected set; }
        public Vector3Control Translation { get; protected set; }
        
        static SpaceNavigatorHID()
        {
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("SpaceNavigator Driver cannot function because the <b>New Input System Package</b> is not active !\n" +
                           "Please enable it in <i>Project Settings/Player/Active Input Handling</i>.");
#endif
            // If no layout with a matching product ID is found, this will be the default. 
            InputSystem.RegisterLayout<SpaceNavigatorHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("3Dconnexion.*")
            );
            DebugLog("SpaceNavigatorHID : RegisterLayout");
        }

        // In the player, trigger the calling of our static constructor
        // by having an empty method annotated with RuntimeInitializeOnLoadMethod.
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
        }

        // FinishSetup is where our device setup is finalized. Here we can look up
        // the controls that have been created.
        protected override void FinishSetup()
        {
            base.FinishSetup();

            displayName = GetType().Name;
            Translation = GetChildControl<Vector3Control>("translation");
            Rotation = GetChildControl<Vector3Control>("rotation");
            Button1 = GetChildControl<ButtonControl>("button1");
            Button2 = GetChildControl<ButtonControl>("button2");

            DebugLog("SpaceNavigatorHID : FinishSetup");
        }

        // We can also expose a '.current' getter equivalent to 'Gamepad.current'.
        // Whenever our device receives input, MakeCurrent() is called. So we can
        // simply update a '.current' getter based on that.
        public static SpaceNavigatorHID current { get; private set; }
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            if (current == this)
                return;
            current = this;
            DebugLog("SpaceNavigatorHID : MakeCurrent");
        }

        // When one of our custom devices is removed, we want to make sure that if
        // it is the '.current' device, we null out '.current'.
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
            DebugLog("SpaceNavigatorHID : OnRemoved");
        }

        public void OnNextUpdate()
        {
        }

        public virtual unsafe void OnStateEvent(InputEventPtr eventPtr)
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

            var newState = default(SpaceNavigatorHIDState);
            // Can opt to only copy the state that we won't override. We don't bother here.
            UnsafeUtility.MemCpy(&newState, (byte*)currentStatePtr + stateBlock.byteOffset, sizeof(SpaceNavigatorHIDState));
            
            if (reportId == 1)
            {
                var reportLength = stateEventPtr->stateSizeInBytes;

                //only contains translation
                if (reportLength == 7)
                    UnsafeUtility.MemCpy(&newState, reportPtr, 7);
                // contains translation and rotation
                else if (reportLength == 13)
                    UnsafeUtility.MemCpy(&newState, reportPtr, 13);

                DebugLog("SpaceNavigatorHID : Copied report1");
            }
            else if (reportId == 2)
            {
                UnsafeUtility.MemCpy(((byte*)&newState) + 7, reportPtr + 1, 6);

                DebugLog("SpaceNavigatorHID : Copied report2");
            }
            else if (reportId == 3)
            {
                UnsafeUtility.MemCpy(&newState.buttonsState, reportStatePtr, sizeof(SpaceNavigatorHIDState.ButtonReport));
                DebugLog("SpaceNavigatorHID : Copied report3");
            }

            // Apply the state change. Don't simply MemCpy over currentStatePtr as that will lead to various
            // malfunctions. The system needs to do the memcpy itself.
            InputState.Change(this, newState, eventPtr: eventPtr);
        }

        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            // If this isn't implemented, some stuff like auto-switching won't work correctly.
            // If you want to implement this, return a state offset corresponding to the control in the given input event.
            return false;
        }

        public static void DebugLog(string _message)
        {
#if SPACENAVIGATOR_DEBUG
            Debug.Log(_message);
#endif
        }
    }
}