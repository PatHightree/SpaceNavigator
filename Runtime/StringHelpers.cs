using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceNavigatorDriver
{
    // Pulled in InputSystem's internal StringHelpers.
    // Removed unneeded parts.
    internal static class StringHelpers
    {
        public static string Join<TValue>(string separator, params TValue[] values)
        {
            return Join(values, separator);
        }

        public static string Join<TValue>(IEnumerable<TValue> values, string separator)
        {
            // Optimize for there not being any values or only a single one
            // that needs no concatenation.
            var firstValue = default(string);
            var valueCount = 0;
            StringBuilder result = null;

            foreach (var value in values)
            {
                if (value == null)
                    continue;
                var str = value.ToString();
                if (string.IsNullOrEmpty(str))
                    continue;

                ++valueCount;
                if (valueCount == 1)
                {
                    firstValue = str;
                    continue;
                }

                if (valueCount == 2)
                {
                    result = new StringBuilder();
                    result.Append(firstValue);
                }

                result.Append(separator);
                result.Append(str);
            }

            if (valueCount == 0)
                return null;
            if (valueCount == 1)
                return firstValue;

            return result.ToString();
        }

        public static string MakeUniqueName<TExisting>(string baseName, IEnumerable<TExisting> existingSet,
            Func<TExisting, string> getNameFunc)
        {
            if (getNameFunc == null)
                throw new ArgumentNullException(nameof(getNameFunc));

            if (existingSet == null)
                return baseName;

            var name = baseName;
            var nameLowerCase = name.ToLower();
            var nameIsUnique = false;
            var namesTried = 1;

            // If the name ends in digits, start counting from the given number.
            if (baseName.Length > 0)
            {
                var lastDigit = baseName.Length;
                while (lastDigit > 0 && char.IsDigit(baseName[lastDigit - 1]))
                    --lastDigit;
                if (lastDigit != baseName.Length)
                {
                    namesTried = int.Parse(baseName.Substring(lastDigit)) + 1;
                    baseName = baseName.Substring(0, lastDigit);
                }
            }

            // Find unique name.
            while (!nameIsUnique)
            {
                nameIsUnique = true;
                foreach (var existing in existingSet)
                {
                    var existingName = getNameFunc(existing);
                    if (existingName.ToLower() == nameLowerCase)
                    {
                        name = $"{baseName}{namesTried}";
                        nameLowerCase = name.ToLower();
                        nameIsUnique = false;
                        ++namesTried;
                        break;
                    }
                }
            }

            return name;
        }
    }
}