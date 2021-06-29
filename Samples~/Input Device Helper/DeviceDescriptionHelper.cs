using UnityEditor;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.HID
{
    internal static class DeviceDescriptionHelper
    {
        static void SaveDeviceDescription(int deviceId, InputDeviceDescription deviceDescription)
        {
            var hidDescriptor = HID.ReadHIDDeviceDescriptor(ref deviceDescription, (ref InputDeviceCommand command) => InputRuntime.s_Instance.DeviceCommand(deviceId, ref command));
            var json = EditorJsonUtility.ToJson(hidDescriptor, true);

            var path = $"Assets/{deviceDescription}.json";
            var fullPath = System.IO.Path.GetFullPath(path).Replace("\\", "/");
            Debug.Log($"Saved ({deviceDescription}) (at {path}:1)\n\n{json}");
            System.IO.File.WriteAllText(path, json);
        }

        [MenuItem("Window/SpaceNavigator/Save all HID descriptors to files", false, 2)]
        static void ListAndSaveAllHIDDevices()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device.description.interfaceName == "HID")
                {
                    SaveDeviceDescription(device.deviceId, device.description);
                }
            }            
        }
    }
}
