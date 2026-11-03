using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExactFertilityGrowingZones
{
    internal static class FertilityValues
    {
        internal const float Tolerance = 0.00001f;

        internal static bool Matches(float actual, float target)
        {
            return Math.Abs(actual - target) <= Tolerance;
        }

        internal static List<float> SortedDistinctPositive(IEnumerable<float> values)
        {
            var sorted = new SortedSet<float>();
            foreach (float value in values)
            {
                if (value > 0f && !float.IsInfinity(value))
                    sorted.Add(value);
            }

            var result = new List<float>();
            foreach (float value in sorted)
            {
                if (result.Count == 0 || !Matches(value, result[result.Count - 1]))
                    result.Add(value);
            }
            return result;
        }

        internal static string Label(float value)
        {
            // Keep fractional custom-terrain fertility visible instead of rounding to whole percent.
            return ((double)value * 100d).ToString("0.###", CultureInfo.CurrentCulture) + "%";
        }
    }
}
