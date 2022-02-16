using System;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using static UnityEngine.InputSystem.HID.HID;

namespace SpaceNavigatorDriver
{
    // Based heavily on InputSystem's HID.HIDLayoutBuilder.
    // Add grouping support.
    // Offset elements' offsets to match merged state.
    [Serializable]
    internal class SpaceMouseHIDLayoutBuilder
    {
        // Define how elements will be grouped together.
        // Also swap y and z positions and invert some axes.
        private static readonly GenericDesktopRangeGroupDefinition[] _genericDesktopRangeGroups = new[]
        {
            new GenericDesktopRangeGroupDefinition
            {
                UsageMin = GenericDesktop.X,
                UsageMax = GenericDesktop.Z,
                Name = "translation",
                DisplayName = "Translation",
                Format = new FourCC('V', 'C', '3', 'S'),
                Layout = "Vector3",
                ElementNames = new string[] { "x", "z", "y" },
                ElementDisplayNames = new string[]{ "X", "Z", "Y" },
                ElementParameters = new string[]{ "", "invert", "invert" }

            },
            new GenericDesktopRangeGroupDefinition
            {
                UsageMin = GenericDesktop.Rx,
                UsageMax = GenericDesktop.Rz,
                Name = "rotation",
                DisplayName = "Rotation",
                Format = new FourCC('V', 'C', '3', 'S'),
                Layout = "Vector3",
                ElementNames = new string[] { "x", "z", "y" },
                ElementDisplayNames = new string[]{ "X", "Z", "Y" },
                ElementParameters = new string[]{ "invert", "", "" }
            }
        };

        internal string displayName;
        internal HIDDeviceDescriptor hidDescriptor;
        internal Type deviceType;
        internal int reportCount;
        internal int reportSize;

        internal InputControlLayout Build()
        {
            var builder = new InputControlLayout.Builder
            {
                displayName = displayName,
                type = deviceType,
                extendsLayout = "HID",
                stateFormat = new FourCC('H', 'I', 'D'),
                stateSizeInBytes = reportCount * reportSize
            };

            // Process HID descriptor.
            var elements = hidDescriptor.elements;
            var elementCount = elements.Length;
            var groupInfo = new GroupInfo();
            for (var i = 0; i < elementCount; ++i)
            {
                ref var element = ref elements[i];
                if (element.reportType != HIDReportType.Input)
                    continue;

                var layout = element.DetermineLayout();
                var usageString = HID.UsageToString(element.usagePage, element.usage);
                if (layout != null)
                {
                    // Check if the element should be placed inside a group control.
                    var hasGroup = TryDetermineGroup(element, ref groupInfo);
                    if (hasGroup)
                    {
                        // Add the group control.
                        // Keep offsets 0 because a sub-controls' offset will be relative to 
                        // the group but we want to keep it relative to the device state.
                        builder.AddControl(groupInfo.Group.Name)
                            .WithDisplayName(groupInfo.Group.DisplayName)
                            .WithFormat(groupInfo.Group.Format)
                            .WithLayout(groupInfo.Group.Layout)
                            .WithByteOffset(0)
                            .WithBitOffset(0);
                    }

                    // Assign unique name.
                    var name = (hasGroup ? groupInfo.ElementName : element.DetermineName());
                    Debug.Assert(!string.IsNullOrEmpty(name));
                    name = StringHelpers.MakeUniqueName(name, builder.controls, x => x.name);

                    // Offset the reported offset to match merged state
                    var offsetInBits = element.reportOffsetInBits + ((element.reportId - 1) * reportSize * 8);

                    DebugLog($"{name}: reportid {element.reportId}, offset {offsetInBits} ({element.reportOffsetInBits}), size: {element.reportSizeInBits}");

                    // Add control.
                    var control =
                        builder.AddControl(name)
                            .WithDisplayName(hasGroup ? groupInfo.ElementDisplayName : element.DetermineDisplayName())
                            .WithLayout(layout)
                            .WithByteOffset((uint)offsetInBits / 8)
                            .WithBitOffset((uint)offsetInBits % 8)
                            .WithSizeInBits((uint)element.reportSizeInBits)
                            .WithFormat(element.DetermineFormat())
                            .WithDefaultState(element.DetermineDefaultState())
                            .WithProcessors(element.DetermineProcessors());

                    var parameters = element.DetermineParameters();
                    if (!string.IsNullOrEmpty(parameters) || (hasGroup && !string.IsNullOrEmpty(groupInfo.ElementParameters)))
                        control.WithParameters(StringHelpers.Join(",", parameters, groupInfo.ElementParameters));

                    var usages = element.DetermineUsages();
                    if (usages != null)
                        control.WithUsages(usages);
                }
            }

            return builder.Build();
        }

        private bool TryDetermineGroup(HIDElementDescriptor e, ref GroupInfo groupInfo)
        {
            for (int i = 0; i < _genericDesktopRangeGroups.Length; i++)
            {
                if (_genericDesktopRangeGroups[i].TryGetGroupInfo(e, ref groupInfo))
                    return true;
            }

            return false;
        }

        private void DebugLog(string msg)
        {
#if SPACENAVIGATOR_DEBUG
            Debug.Log($"{nameof(SpaceMouseHIDLayoutBuilder)}: {msg}");
#endif
        }

        /// <summary>
        /// Helps to create group controls from consecutive elements
        /// </summary>
        private class GenericDesktopRangeGroupDefinition
        {
            public GenericDesktop UsageMin;
            public GenericDesktop UsageMax;
            public string Name;
            public string DisplayName;
            public FourCC Format;
            public string Layout;
            public string[] ElementNames;
            public string[] ElementDisplayNames;
            public string[] ElementParameters;

            public bool TryGetGroupInfo(HIDElementDescriptor e, ref GroupInfo groupInfo)
            {
                if (e.usagePage != UsagePage.GenericDesktop)
                    return false;

                if (e.usage < (int)UsageMin || e.usage > (int)UsageMax)
                    return false;

                int elementIndex = e.usage - (int)UsageMin;
                groupInfo = new GroupInfo
                {
                    Group = this,
                    ElementName = Name + "/" + ElementNames[elementIndex],
                    ElementDisplayName = ElementDisplayNames[elementIndex],
                    ElementParameters = ElementParameters == null ? null : ElementParameters[elementIndex]
                };

                return true;
            }
        }

        /// <summary>
        /// Holds the grouping information for one specific element
        /// </summary>
        private struct GroupInfo
        {
            public GenericDesktopRangeGroupDefinition Group;
            public string ElementName;
            public string ElementDisplayName;
            public string ElementParameters;
        }
    }
}