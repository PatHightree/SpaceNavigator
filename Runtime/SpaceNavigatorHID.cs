using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceNavigatorDriver
{
    struct SpaceNavigatorHIDState : IInputStateTypeInfo
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
        public ButtonControl Button1 { get; protected set; }
        public ButtonControl Button2 { get; protected set; }
        public Vector3Control Rotation { get; protected set; }
        public Vector3Control Translation { get; protected set; }
        
        static SpaceNavigatorHID()
        {
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("SpaceNavigator Driver cannot function because the <b>New Input System Package</b> is not active!\n" +
                           "Please enable it in <i>Project Settings/Player/Active Input Handling</i>.");
#endif
            // If no layout with a matching product ID is found, this will be the default. 
            InputSystem.RegisterLayout<SpaceNavigatorHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("3Dconnexion.*")
            );
#if UNITY_EDITOR
            EditorApplication.quitting += Quit;
#else
            Application.quitting += Quit;
#endif
            DebugLog("SpaceNavigatorHID : RegisterLayout");
        }

        // In the player, trigger the calling of our static constructor
        // by having an empty method annotated with RuntimeInitializeOnLoadMethod.
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
        }

        private static void Quit()
        {
            current.SetLEDStatus(LedStatus.Off);
        }
        
        // FinishSetup is where our device setup is finalized. Here we can look up
        // the controls that have been created.
        protected override void FinishSetup()
        {
            base.FinishSetup();

            displayName = GetType().Name;
            Button1 = GetChildControl<ButtonControl>("button1");
            Button2 = GetChildControl<ButtonControl>("button2");
            Rotation = GetChildControl<Vector3Control>("rotation");
            Translation = GetChildControl<Vector3Control>("translation");

            DebugLog("SpaceNavigatorHID : FinishSetup");
        }

        // We can also expose a '.current' getter equivalent to 'Gamepad.current'.
        // Whenever our device receives input, MakeCurrent() is called. So we can
        // simply update a '.current' getter based on that.
        public static SpaceNavigatorHID current { get; private set; }
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
            SetLEDStatus(LedStatus.On);
            DebugLog("SpaceNavigatorHID : MakeCurrent");
        }

        // When one of our custom devices is removed, we want to make sure that if
        // it is the '.current' device, we null out '.current'.
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
            SetLEDStatus(LedStatus.Off);
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
            UnsafeUtility.MemCpy(&newState, (byte*) currentStatePtr + stateBlock.byteOffset, sizeof(SpaceNavigatorHIDState));

            if (reportId == 1)
            {
                UnsafeUtility.MemCpy(&newState.report1, reportStatePtr, sizeof(SpaceNavigatorHIDState.ReportFormat1));
                DebugLog("SpaceNavigatorHID : Copied report1");
            }
            else if (reportId == 2)
            {
                UnsafeUtility.MemCpy(&newState.report2, reportStatePtr, sizeof(SpaceNavigatorHIDState.ReportFormat2));
                DebugLog("SpaceNavigatorHID : Copied report2");
            }
            else if (reportId == 3)
            {
                UnsafeUtility.MemCpy(&newState.report3, reportStatePtr, sizeof(SpaceNavigatorHIDState.ReportFormat3));
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

#region Status LED

        public void SetLEDStatus(LedStatus status)
        {
            var cmd = LEDCommand.Create(status);
            var result = ExecuteCommand(ref cmd);
            DebugLog($"SpaceNavigatorHID : Executed LEDCommand. status = {status}, result = {result}");
        }

        public enum LedStatus { Off = 0, On = 1 }

        [StructLayout(LayoutKind.Explicit)]
        private struct LEDCommand : IInputDeviceCommandInfo
        {
            public FourCC typeStatic => new FourCC('H', 'I', 'D', 'O');

            [FieldOffset(0)]
            public InputDeviceCommand baseCommand;
            [FieldOffset(InputDeviceCommand.BaseCommandSize)]
            public byte reportId;
            [FieldOffset(InputDeviceCommand.BaseCommandSize + 1)]
            public byte status;

            public static LEDCommand On => Create(LedStatus.On);
            public static LEDCommand Off => Create(LedStatus.Off);

            public static LEDCommand Create(LedStatus status)
            {
                var result = new LEDCommand();
                result.baseCommand = new InputDeviceCommand(result.typeStatic, InputDeviceCommand.BaseCommandSize + 2);
                result.reportId = 0x04;
                result.status = (byte)status;
                return result;
            }
        }

#endregion

#region Utilities

        public static void DebugLog(string _message)
        {
#if SPACENAVIGATOR_DEBUG
            Debug.Log(_message);
#endif
        }

#endregion
    }
}
