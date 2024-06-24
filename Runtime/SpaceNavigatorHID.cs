using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceNavigatorDriver
{
#if UNITY_EDITOR
    [InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
    public class SpaceNavigatorHID : InputDevice
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
            InputSystem.RegisterLayout<SpaceNavigatorHID>();
            InputSystem.RegisterPrecompiledLayout<FastHID>(FastHID.metadata);
            InputSystem.AddDevice<SpaceNavigatorHID>();
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
            if (current != null)
                current.SetLEDStatus(LedStatus.Off);
        }
        
        // FinishSetup is where our device setup is finalized. Here we can look up
        // the controls that have been created.
        protected override void FinishSetup()
        {
            base.FinishSetup();

            displayName = GetType().Name;
            Button1 = GetChildControl<ButtonControl>("trigger");
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