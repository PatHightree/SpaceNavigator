using UnityEditor;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace SpaceNavigatorDriver
{
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

    struct SpaceNavigatorHIDState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('H', 'I', 'D');

        // 1st report
        [InputControl(name = "translation", format = "VC3S", layout = "Vector3", displayName = "Translation")] 
        [InputControl(name = "translation/x", offset = 0, format = "SHRT", parameters = "scale=true, scaleFactor=10")] 
        [InputControl(name = "translation/y", offset = 4, format = "SHRT", parameters = "scale=true, scaleFactor=-10")]
        [InputControl(name = "translation/z", offset = 2, format = "SHRT", parameters = "scale=true, scaleFactor=-10")]
        public ReportFormat1 report1;

        // 2nd report
        [InputControl(name = "rotation", format = "VC3S", layout = "Vector3", displayName = "Rotation")] 
        [InputControl(name = "rotation/x", offset = 0, format = "SHRT", parameters = "scale=true, scaleFactor=-80")] 
        [InputControl(name = "rotation/y", offset = 4, format = "SHRT", parameters = "scale=true, scaleFactor=80")] 
        [InputControl(name = "rotation/z", offset = 2, format = "SHRT", parameters = "scale=true, scaleFactor=80")]
        public ReportFormat2 report2;

        // 3rd report
        [InputControl(name = "button1", bit = 0, format = "BIT", layout = "Button", displayName = "Button 1")] 
        [InputControl(name = "button2", bit = 1, format = "BIT", layout = "Button", displayName = "Button 2")]
        public ReportFormat3 report3;
    }

#if UNITY_EDITOR
    [InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
    [InputControlLayout(stateType = typeof(SpaceNavigatorHIDState))]
    public class SpaceNavigatorHID : InputDevice, IInputStateCallbackReceiver
    {
        public ButtonControl Button1 { get; private set; }
        public ButtonControl Button2 { get; private set; }
        public Vector3Control Rotation { get; private set; }
        public Vector3Control Translation { get; private set; }

        static SpaceNavigatorHID()
        {
            InputSystem.RegisterLayout<SpaceNavigatorHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("3Dconnexion")
                    .WithProduct(".*"));
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

            Button1 = GetChildControl<ButtonControl>("button1");
            Button2 = GetChildControl<ButtonControl>("button2");
            Rotation = GetChildControl<Vector3Control>("rotation");
            Translation = GetChildControl<Vector3Control>("translation");
        }

        // We can also expose a '.current' getter equivalent to 'Gamepad.current'.
        // Whenever our device receives input, MakeCurrent() is called. So we can
        // simply update a '.current' getter based on that.
        public static SpaceNavigatorHID current { get; private set; }
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        // When one of our custom devices is removed, we want to make sure that if
        // it is the '.current' device, we null out '.current'.
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }
        public void OnNextUpdate()
        {
        }

        public unsafe void OnStateEvent(InputEventPtr eventPtr)
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
            UnsafeUtility.MemCpy(&newState, (byte*) currentStatePtr + stateBlock.byteOffset, sizeof(SpaceNavigatorHIDState));

            if (reportId == 1)
                UnsafeUtility.MemCpy(&newState.report1, reportStatePtr, sizeof(ReportFormat1));
            else if (reportId == 2)
                UnsafeUtility.MemCpy(&newState.report2, reportStatePtr, sizeof(ReportFormat2));
            else if (reportId == 3)
                UnsafeUtility.MemCpy(&newState.report3, reportStatePtr, sizeof(ReportFormat3));

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
    }
}