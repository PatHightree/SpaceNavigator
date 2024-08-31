using System.Runtime.CompilerServices;

namespace SpaceNavigatorDriver
{
    // Pulled in InputSystem's internal NumberHelpers.
    // Removed unneeded parts.
    internal static class NumberHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float IntToNormalizedFloat(int value, int minValue, int maxValue)
        {
            if (value <= minValue)
                return 0.0f;
            if (value >= maxValue)
                return 1.0f;
            // using double here because int.MaxValue is not representable in floats
            // as int.MaxValue = 2147483647 will become 2147483648.0 when casted to a float
            return (float)(((double)value - minValue) / ((double)maxValue - minValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UIntToNormalizedFloat(uint value, uint minValue, uint maxValue)
        {
            if (value <= minValue)
                return 0.0f;
            if (value >= maxValue)
                return 1.0f;
            // using double here because uint.MaxValue is not representable in floats
            return (float)(((double)value - minValue) / ((double)maxValue - minValue));
        }
    }
}