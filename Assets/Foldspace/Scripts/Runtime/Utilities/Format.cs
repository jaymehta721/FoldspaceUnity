using UnityEngine;

namespace Foldspace.Utilities
{
    public static class Format
    {
        /// <summary>Minutes and seconds, e.g. 2:07.</summary>
        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
