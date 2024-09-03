using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using static UnityEngine.InputSystem.HID.HID;

namespace SpaceNavigatorDriver
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class SpaceMouseHIDSetup
    {
        private static bool _isInited;
        private static int _waitCounter = 0;

        static SpaceMouseHIDSetup()
        {
            Init();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init()
        {
            if (_isInited)
                return;

            _isInited = true;

#if UNITY_EDITOR
            // Hack to delay the actual initialization by 1 frame _only_ right after editor startup. Why:
            // If we don't delay after startup the auto-generated HID layout will be applied (until domain reload).
            // If we delay all the time there'll be some ugly InvalidCastExceptions because of layout mismatches.
            if (EditorApplication.isPlayingOrWillChangePlaymode == false &&
                InputSystem.devices.Any(d => MatchDeviceShallowPass(d.description)) == false)
            {
                _waitCounter = 1;
                EditorApplication.update += LateInit;
                return;
            }
#endif

            DoInit();
        }

#if UNITY_EDITOR
        private static void LateInit()
        {
            if (_waitCounter-- > 0)
                return;

            EditorApplication.update -= LateInit;

            DoInit();
        }
#endif

        private static void DoInit()
        {
            RemoveExistingLayout();

            // Register our handler.
            DebugLog("Register onFindLayoutForDevice handler");
            InputSystem.onFindLayoutForDevice += InputSystem_onFindLayoutForDevice;

            /*
            // Find existing matching devices and re-add them.
            // This is a hack to make the InputSystem actually use our handler right after editor startup.
            // Without this, it needs an assembly reload (e.g. by going into play mode or recompiling)
            // to switch from the auto-generated HID layout to our custom-tailored one.
            var existingDevices = InputSystem.devices
                .Where(d => MatchDeviceShallowPass(d.description));

            foreach (var existingDevice in existingDevices)
            {
                DebugLog("Re-adding device: " + existingDevice);

                var description = existingDevice.description;
                InputSystem.DisableDevice(existingDevice);
                InputSystem.AddDevice(description);
            }
            */
        }

        private static void RemoveExistingLayout()
        {
            var existingLayouts = InputSystem.ListLayouts()
                            .Where(l => l.StartsWith("HID::") && l.Contains("connex"))
                            .ToList();

            var devices = InputSystem.devices
                .Where(d => existingLayouts.Contains(d.layout))
                .ToList();

            foreach (var existingLayout in existingLayouts)
            {
                DebugLog("Removing layout: " + existingLayout);

                InputSystem.RemoveLayout(existingLayout);
            }

            //foreach(var device in devices)
            //{
            //    DebugLog("Device used removed layout: " + device);

            //    InputSystem.EnableDevice(device);
            //}
        }

        /// <summary>
        /// Should be fast in recognizing possible matching devices.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="executeDeviceCommand"></param>
        /// <returns></returns>
        private static bool MatchDeviceShallowPass(InputDeviceDescription description)
        {
            if (description.interfaceName != "HID")
                return false;

            // Use the supplied descriptor here.
            // It might still suffice to identify the device as non-matching.
            var hidDescriptor = HIDDeviceDescriptor.FromJson(description.capabilities);

            // TODO: Match against list of compatible vendor and product ids, maybe from https://github.com/openantz/antz/wiki/3D-Mouse#developers
            if (hidDescriptor.vendorId != 0x256f)
                return false;

            return true;
        }

        /// <summary>
        /// Performs a more thorough analysis of the device.
        /// The <paramref name="description"/>'s capabilities field might be set by this method if needed.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="executeDeviceCommand"></param>
        /// <returns></returns>
        private static bool MatchDeviceDeepPass(ref InputDeviceDescription description, ref HIDDeviceDescriptor hidDescriptor, InputDeviceExecuteCommandDelegate executeDeviceCommand)
        {
            hidDescriptor = HIDDeviceDescriptor.FromJson(description.capabilities);

            // Read the full descriptor if it does not contain any elements
            // We need the elements to construct the layout
            if (hidDescriptor.elements == null || hidDescriptor.elements.Length == 0)
                hidDescriptor = HIDHelpers.ReadHIDDeviceDescriptor(ref description, executeDeviceCommand);

            if (hidDescriptor.elements == null)
            {
                Debug.LogError("Could not read HID descriptor");
                return false;
            }

            var reportSizeMap = hidDescriptor.elements
                .GroupBy(e => e.reportId)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => 1 + Mathf.CeilToInt(g.Sum(e => e.reportSizeInBits) / 8f)); // All elements' sizes aligned to bytes + 1 byte for reportId

            // Check if reports are in a valid format to be mapped to our merged state struct
            //if (reportSizeMap.Keys.Any(id => id < 1 || id > SpaceNavigatorHID.ReportCountMax) ||
            //    reportSizeMap.Values.Any(s => s > SpaceNavigatorHID.ReportSizeMax))
            //{
            //    Debug.LogWarning($"{nameof(SpaceMouseHIDSetup)}: This device reports its state in an incompatible format, please report an issue with following info:\n{hidDescriptor.ToJson()}");
            //    return false;
            //}

#if SPACENAVIGATOR_DEBUG
            foreach (var rc in reportSizeMap)
                DebugLog($"Report {rc.Key}: size in bits: {rc.Value}");
#endif

            return true;
        }

        private static string InputSystem_onFindLayoutForDevice(ref InputDeviceDescription description, string matchedLayout, InputDeviceExecuteCommandDelegate executeDeviceCommand)
        {
            HIDDeviceDescriptor hidDescriptor = new HIDDeviceDescriptor();

            if (MatchDeviceShallowPass(description) == false)
                return null;

            if (MatchDeviceDeepPass(ref description, ref hidDescriptor, executeDeviceCommand) == false)
                return null;

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(matchedLayout) == false && matchedLayout.StartsWith("HID::"))
            {
                _waitCounter = 1;
                EditorApplication.update += DelayRemove;
            }
#endif

            var deviceMatcher = InputDeviceMatcher.FromDeviceDescription(description);
            var layoutName = $"SPACEMOUSE::{description.manufacturer} {description.product}";
            var baseType = typeof(SpaceNavigatorHID);

            DebugLog($"Register layout builder for {description.product} and descriptor:\n{hidDescriptor.ToJson()}");
            var layout = new SpaceMouseHIDLayoutBuilder
            {
                displayName = description.product,
                hidDescriptor = hidDescriptor,
                deviceType = baseType,
                reportCount = SpaceNavigatorHID.ReportCountMax,
                reportSize = SpaceNavigatorHID.ReportSizeMax
            };
            InputSystem.RegisterLayoutBuilder(() => layout.Build(),
                layoutName, "HID", deviceMatcher);

            return layoutName;
        }

#if UNITY_EDITOR
        private static void DelayRemove()
        {
            if (_waitCounter-- > 0)
                return;

            EditorApplication.update -= DelayRemove;

            RemoveExistingLayout();
        }
#endif

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerNonUserCode]
        [System.Diagnostics.DebuggerStepThrough]
        private static void DebugLog(string msg)
        {
#if SPACENAVIGATOR_DEBUG
            Debug.Log($"{nameof(SpaceMouseHIDSetup)}: {msg}");
#endif
        }
    }
}