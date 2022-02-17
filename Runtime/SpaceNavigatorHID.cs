using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace SpaceNavigatorDriver
{
    //[InputControlLayout(stateType = typeof(MergedReports))]
    public class SpaceNavigatorHID : HID, IInputStateCallbackReceiver
    {
        internal const int ReportSizeMax = 33;
        internal const int ReportCountMax = 3;
        internal const int StateSizeMax = ReportSizeMax * ReportCountMax;

        [StructLayout(LayoutKind.Explicit, Size = StateSizeMax)]
        public struct MergedReports
        { }

        public static SpaceNavigatorHID current { get; private set; }

        public ButtonControl Button1 { get; protected set; }
        public ButtonControl Button2 { get; protected set; }
        public Vector3Control Rotation { get; protected set; }
        public Vector3Control Translation { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            Button1 = GetChildControl<ButtonControl>("button1");
            Button2 = GetChildControl<ButtonControl>("button2");
            Rotation = GetChildControl<Vector3Control>("rotation");
            Translation = GetChildControl<Vector3Control>("translation");
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();

            if (current == this)
                return;

            current = this;

            DebugLog($"Current instance: {displayName}");
        }

        public void OnNextUpdate()
        { }

        public unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            if (eventPtr.IsA<DeltaStateEvent>())
                return;

            var stateEventPtr = StateEvent.From(eventPtr);
            if (stateEventPtr->stateFormat != new FourCC('H', 'I', 'D'))
                return;

            var reportPtr = (byte*)stateEventPtr->state;
            var reportId = *reportPtr;

            if (reportId < 1 || reportId > ReportCountMax)
                return;

            // Get pointer to current state.
            var newState = (byte*)currentStatePtr + stateBlock.byteOffset;

            // Merge incoming report into the current state.
            // Use reportId to map to a specific block inside the state struct.
            var offset = (uint)(ReportSizeMax * (reportId - 1));
            var length = stateEventPtr->stateSizeInBytes;
            var maxLength = (stateBlock.sizeInBits + 7) >> 3;
            // Make sure not to not exceed state block boundaries. Its size is not equal to the state's struct size!
            // Guess: Size might be calculated by last element offset + last element size, byte-aligned.
            if (offset + length > maxLength)
                length = maxLength - offset;
            UnsafeUtility.MemCpy(newState + offset, reportPtr, length);
            //DebugLog($"Copied report {reportId} {stateEventPtr->stateSizeInBytes}:\n" + Hex(&newState, StateSizeMax, ReportSizeMax));

            InputState.Change(this, *(MergedReports*)newState);
        }

        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            // TODO: How to implement this?
            return false;
        }

        internal static void DebugLog(string msg)
        {
#if SPACENAVIGATOR_DEBUG
            Debug.Log($"{nameof(SpaceNavigatorHID)}: {msg}");
#endif
        }

#if SPACENAVIGATOR_DEBUG
        protected override unsafe long ExecuteCommand(InputDeviceCommand* commandPtr)
        {
            var type = commandPtr->type;
            DebugLog($"ExecuteCommand: {type}");
            return base.ExecuteCommand(commandPtr);
        }

        private unsafe string Hex(void* newState, int length, int stride = -1)
        {
            byte[] b = new byte[length];
            fixed (byte* ptr = &b[0])
            {
                UnsafeUtility.MemCpy(ptr, newState, length);
            }

            if (stride < 0)
                stride = length;

            var hexString = string.Join(
                "\n",
                Enumerable.Range(0, length / stride)
                    .Select(i => BitConverter.ToString(b.Skip(i * stride).Take(stride).ToArray()).Replace('-', ' ')));

            return hexString;
        }
#endif
    }
}