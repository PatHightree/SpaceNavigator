using System;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using static UnityEngine.InputSystem.HID.HID;

namespace SpaceNavigatorDriver
{
    internal static class HIDHelpers
    {
        // Pulled in from InputSystem's HID.cs.
        // Modified to always go the binary descriptor route to avoid to pull in even more HID-internal code.
        public static unsafe HIDDeviceDescriptor ReadHIDDeviceDescriptor(ref InputDeviceDescription deviceDescription, InputDeviceExecuteCommandDelegate executeCommandDelegate)
        {
            var hidDeviceDescriptor = new HIDDeviceDescriptor();

            // The device may not support binary descriptors but may support parsed descriptors so
            // try the IOCTL for parsed descriptors next.
            //
            // This path exists pretty much only for the sake of Windows where it is not possible to get
            // unparsed/binary descriptors from the device (and where getting element offsets is only possible
            // with some dirty hacks we're performing in the native runtime).

            const int kMaxDescriptorBufferSize = 2 * 1024 * 1024; ////TODO: switch to larger buffer based on return code if request fails
            using (var buffer =
                        InputDeviceCommand.AllocateNative(QueryHIDParsedReportDescriptorDeviceCommandType, kMaxDescriptorBufferSize))
            {
                var commandPtr = (InputDeviceCommand*)buffer.GetUnsafePtr();
                var utf8Length = executeCommandDelegate(ref *commandPtr);
                if (utf8Length < 0)
                    return new HIDDeviceDescriptor();

                // Turn UTF-8 buffer into string.
                ////TODO: is there a way to not have to copy here?
                var utf8 = new byte[utf8Length];
                fixed (byte* utf8Ptr = utf8)
                {
                    UnsafeUtility.MemCpy(utf8Ptr, commandPtr->payloadPtr, utf8Length);
                }
                var descriptorJson = Encoding.UTF8.GetString(utf8, 0, (int)utf8Length);

                // Try to parse the HID report descriptor.
                try
                {
                    hidDeviceDescriptor = HIDDeviceDescriptor.FromJson(descriptorJson);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Could not parse HID descriptor of device '{deviceDescription}'");
                    Debug.LogException(exception);
                    return new HIDDeviceDescriptor();
                }

                // Update the descriptor on the device with the information we got.
                deviceDescription.capabilities = descriptorJson;
            }

            return hidDeviceDescriptor;
        }
    }
}