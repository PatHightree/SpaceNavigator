using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using static UnityEngine.InputSystem.HID.HID;

namespace SpaceNavigatorDriver
{
    // Pulled in HID-internal helpers from HID.HIDElementDescriptor.
    // Converted to extension methods.
    // Slightly modified in some places to e.g. not call the first button "Trigger".
    // Removed unneeded parts.
    static class HIDElementDescriptorExtensions
    {
        public static bool isSigned(this HIDElementDescriptor e) => e.logicalMin < 0;

        public static float minFloatValue(this HIDElementDescriptor e)
        {
            if (e.isSigned())
            {
                var minValue = (int)-(long)(1UL << (e.reportSizeInBits - 1));
                var maxValue = (int)((1UL << (e.reportSizeInBits - 1)) - 1);
                return NumberHelpers.IntToNormalizedFloat(e.logicalMin, minValue, maxValue) * 2.0f - 1.0f;
            }
            else
            {
                Debug.Assert(e.logicalMin >= 0, $"Expected logicalMin to be unsigned");
                var maxValue = (uint)((1UL << e.reportSizeInBits) - 1);
                return NumberHelpers.UIntToNormalizedFloat((uint)e.logicalMin, 0, maxValue);
            }
        }

        public static float maxFloatValue(this HIDElementDescriptor e)
        {
            if (e.isSigned())
            {
                var minValue = (int)-(long)(1UL << (e.reportSizeInBits - 1));
                var maxValue = (int)((1UL << (e.reportSizeInBits - 1)) - 1);
                return NumberHelpers.IntToNormalizedFloat(e.logicalMax, minValue, maxValue) * 2.0f - 1.0f;
            }
            else
            {
                Debug.Assert(e.logicalMax >= 0, $"Expected e.logicalMax to be unsigned");
                var maxValue = (uint)((1UL << e.reportSizeInBits) - 1);
                return NumberHelpers.UIntToNormalizedFloat((uint)e.logicalMax, 0, maxValue);
            }
        }

        public static string DetermineName(this HIDElementDescriptor e)
        {
            // It's rare for HIDs to declare string names for items and HID drivers may report weird strings
            // plus there's no guarantee that these names are unique per item. So, we don't bother here with
            // device/driver-supplied names at all but rather do our own naming.

            switch (e.usagePage)
            {
                case UsagePage.Button:
                    return $"button{e.usage}";
                case UsagePage.GenericDesktop:
                    if (e.usage == (int)GenericDesktop.HatSwitch)
                        return "hat";
                    var text = ((GenericDesktop)e.usage).ToString();
                    // Lower-case first letter.
                    text = char.ToLowerInvariant(text[0]) + text.Substring(1);
                    return text;
            }

            // Fallback that generates a somewhat useless but at least very informative name.
            return $"UsagePage({e.usagePage:X}) Usage({e.usage:X})";
        }

        public static string DetermineDisplayName(this HIDElementDescriptor e)
        {
            switch (e.usagePage)
            {
                case UsagePage.Button:
                    return $"Button {e.usage}";
                case UsagePage.GenericDesktop:
                    return ((GenericDesktop)e.usage).ToString();
            }

            return null;
        }

        public static string DetermineLayout(this HIDElementDescriptor e)
        {
            if (e.reportType != HIDReportType.Input)
                return null;

            ////TODO: deal with arrays

            switch (e.usagePage)
            {
                case UsagePage.Button:
                    return "Button";
                case UsagePage.GenericDesktop:
                    switch (e.usage)
                    {
                        case (int)GenericDesktop.X:
                        case (int)GenericDesktop.Y:
                        case (int)GenericDesktop.Z:
                        case (int)GenericDesktop.Rx:
                        case (int)GenericDesktop.Ry:
                        case (int)GenericDesktop.Rz:
                        case (int)GenericDesktop.Vx:
                        case (int)GenericDesktop.Vy:
                        case (int)GenericDesktop.Vz:
                        case (int)GenericDesktop.Vbrx:
                        case (int)GenericDesktop.Vbry:
                        case (int)GenericDesktop.Vbrz:
                        case (int)GenericDesktop.Slider:
                        case (int)GenericDesktop.Dial:
                        case (int)GenericDesktop.Wheel:
                            return "Axis";

                        case (int)GenericDesktop.Select:
                        case (int)GenericDesktop.Start:
                        case (int)GenericDesktop.DpadUp:
                        case (int)GenericDesktop.DpadDown:
                        case (int)GenericDesktop.DpadLeft:
                        case (int)GenericDesktop.DpadRight:
                            return "Button";

                        case (int)GenericDesktop.HatSwitch:
                            // Only support hat switches with 8 directions.
                            if (e.logicalMax - e.logicalMin + 1 == 8)
                                return "Dpad";
                            break;
                    }
                    break;
            }

            return null;
        }

        public static FourCC DetermineFormat(this HIDElementDescriptor e)
        {
            switch (e.reportSizeInBits)
            {
                case 8:
                    return e.isSigned() ? InputStateBlock.FormatSByte : InputStateBlock.FormatByte;
                case 16:
                    return e.isSigned() ? InputStateBlock.FormatShort : InputStateBlock.FormatUShort;
                case 32:
                    return e.isSigned() ? InputStateBlock.FormatInt : InputStateBlock.FormatUInt;
                default:
                    // Generic bitfield value.
                    return InputStateBlock.FormatBit;
            }
        }

        public static InternedString[] DetermineUsages(this HIDElementDescriptor e)
        {
            if (e.usagePage == UsagePage.Button && e.usage == 1)
                return new[] { CommonUsages.PrimaryTrigger, CommonUsages.PrimaryAction };
            if (e.usagePage == UsagePage.Button && e.usage == 2)
                return new[] { CommonUsages.SecondaryTrigger, CommonUsages.SecondaryAction };
            if (e.usagePage == UsagePage.GenericDesktop && e.usage == (int)GenericDesktop.Rz)
                return new[] { CommonUsages.Twist };
            ////TODO: assign hatswitch usage to first and only to first hatswitch element
            return null;
        }

        public static string DetermineParameters(this HIDElementDescriptor e)
        {
            if (e.usagePage == UsagePage.GenericDesktop)
            {
                switch (e.usage)
                {
                    case (int)GenericDesktop.X:
                    case (int)GenericDesktop.Y:
                    case (int)GenericDesktop.Z:
                    case (int)GenericDesktop.Rx:
                    case (int)GenericDesktop.Ry:
                    case (int)GenericDesktop.Rz:
                    case (int)GenericDesktop.Vx:
                    case (int)GenericDesktop.Vy:
                    case (int)GenericDesktop.Vz:
                    case (int)GenericDesktop.Vbrx:
                    case (int)GenericDesktop.Vbry:
                    case (int)GenericDesktop.Vbrz:
                    case (int)GenericDesktop.Slider:
                    case (int)GenericDesktop.Dial:
                    case (int)GenericDesktop.Wheel:
                        return e.DetermineAxisNormalizationParameters();
                }
            }

            return null;
        }

        public static string DetermineAxisNormalizationParameters(this HIDElementDescriptor e)
        {
            // If we have min/max bounds on the axis values, set up normalization on the axis.
            // NOTE: We put the center in the middle between min/max as we can't know where the
            //       resting point of the axis is (may be on min if it's a trigger, for example).
            if (e.logicalMin == 0 && e.logicalMax == 0)
                return "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5";
            var min = e.minFloatValue();
            var max = e.maxFloatValue();
            // Do nothing if result of floating-point conversion is already normalized.
            if (Mathf.Approximately(0f, min) && Mathf.Approximately(0f, max))
                return null;
            var zero = min + (max - min) / 2.0f;
            return string.Format(CultureInfo.InvariantCulture, "normalize,normalizeMin={0},normalizeMax={1},normalizeZero={2}", min, max, zero);
        }

        public static string DetermineProcessors(this HIDElementDescriptor e)
        {
            switch (e.usagePage)
            {
                case UsagePage.GenericDesktop:
                    switch (e.usage)
                    {
                        case (int)GenericDesktop.X:
                        case (int)GenericDesktop.Y:
                        case (int)GenericDesktop.Z:
                        case (int)GenericDesktop.Rx:
                        case (int)GenericDesktop.Ry:
                        case (int)GenericDesktop.Rz:
                        case (int)GenericDesktop.Vx:
                        case (int)GenericDesktop.Vy:
                        case (int)GenericDesktop.Vz:
                        case (int)GenericDesktop.Vbrx:
                        case (int)GenericDesktop.Vbry:
                        case (int)GenericDesktop.Vbrz:
                        case (int)GenericDesktop.Slider:
                        case (int)GenericDesktop.Dial:
                        case (int)GenericDesktop.Wheel:
                            return "axisDeadzone";
                    }
                    break;
            }

            return null;
        }

        public static PrimitiveValue DetermineDefaultState(this HIDElementDescriptor e)
        {
            switch (e.usagePage)
            {
                case UsagePage.GenericDesktop:
                    switch (e.usage)
                    {
                        case (int)GenericDesktop.HatSwitch:
                            // Figure out null state for hat switches.
                            if (e.hasNullState)
                            {
                                // We're looking for a value that is out-of-range with respect to the
                                // logical min and max but in range with respect to what we can store
                                // in the bits we have.

                                // Test lower bound, we can store >= 0.
                                if (e.logicalMin >= 1)
                                    return new PrimitiveValue(e.logicalMin - 1);

                                // Test upper bound, we can store <= maxValue.
                                var maxValue = (1UL << e.reportSizeInBits) - 1;
                                if ((ulong)e.logicalMax < maxValue)
                                    return new PrimitiveValue(e.logicalMax + 1);
                            }
                            break;

                        case (int)GenericDesktop.X:
                        case (int)GenericDesktop.Y:
                        case (int)GenericDesktop.Z:
                        case (int)GenericDesktop.Rx:
                        case (int)GenericDesktop.Ry:
                        case (int)GenericDesktop.Rz:
                        case (int)GenericDesktop.Vx:
                        case (int)GenericDesktop.Vy:
                        case (int)GenericDesktop.Vz:
                        case (int)GenericDesktop.Vbrx:
                        case (int)GenericDesktop.Vbry:
                        case (int)GenericDesktop.Vbrz:
                        case (int)GenericDesktop.Slider:
                        case (int)GenericDesktop.Dial:
                        case (int)GenericDesktop.Wheel:
                            // For axes that are *NOT* stored as signed values (which we assume are
                            // centered on 0), put the default state in the middle between the min and max.
                            if (!e.isSigned())
                            {
                                var defaultValue = e.logicalMin + (e.logicalMax - e.logicalMin) / 2;
                                if (defaultValue != 0)
                                    return new PrimitiveValue(defaultValue);
                            }
                            break;
                    }
                    break;
            }

            return new PrimitiveValue();
        }
    }
}