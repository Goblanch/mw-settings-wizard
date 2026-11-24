using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MiradorWorks.Settings
{
    internal readonly struct ResolutionOption
    {
        public ResolutionOption(string key, string label, Resolution resolution)
        {
            Key = key;
            Label = label;
            Resolution = resolution;
        }

        public string Key { get; }
        public string Label { get; }
        public Resolution Resolution { get; }
    }

    /// <summary>
    /// Pure resolution logic, kept apart from Screen so it can be tested without a display.
    /// Keys look like "1920x1080@144", or "1920x1080" when refresh rates are not listed.
    /// </summary>
    internal static class ResolutionOptions
    {
        /// <summary>
        /// Builds the dropdown options: duplicates removed (the highest refresh rate wins),
        /// sizes below the minimim height dropped (unless that drops all of them), largest first.
        /// </summary>
        public static List<ResolutionOption> Build(IEnumerable<Resolution> available, int minimumHeight, bool includeRefreshRate)
        {
            var all = new List<Resolution>(available ?? Array.Empty<Resolution>());

            bool anyTallEnough = false;
            foreach (Resolution resolution in all)
            {
                if (resolution.height >= minimumHeight)
                {
                    anyTallEnough = true;
                    break;
                }
            }

            var byKey = new Dictionary<string, Resolution>(StringComparer.Ordinal);
            foreach (Resolution resolution in all)
            {
                if (resolution.width <= 0 || resolution.height <= 0)
                    continue;
                if (anyTallEnough && resolution.height < minimumHeight)
                    continue;

                string key = GetKey(resolution.width, resolution.height, RoundHz(resolution.refreshRateRatio), includeRefreshRate);
                if (!byKey.TryGetValue(key, out Resolution existing)
                    || resolution.refreshRateRatio.value > existing.refreshRateRatio.value)
                {
                    byKey[key] = resolution;
                }
            }

            var options = new List<ResolutionOption>(byKey.Count);
            foreach (KeyValuePair<string, Resolution> pair in byKey)
            {
                Resolution r = pair.Value;
                options.Add(new ResolutionOption(
                    pair.Key, GetLabel(r.width, r.height, RoundHz(r.refreshRateRatio), includeRefreshRate), r));
            }

            options.Sort(CompareLargestFirst);
            return options;
        }

        public static string GetKey(int width, int height, int hz, bool includeRefreshRate)
        {
            string size = width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
            return includeRefreshRate && hz > 0 ? size + "@" + hz.ToString(CultureInfo.InvariantCulture) : size;
        }

        public static string GetLabel(int width, int height, int hz, bool includeRefreshRate)
        {
            string size = $"{width} x {height}";
            return includeRefreshRate && hz > 0 ? $"{size} @ {hz}Hz" : size;
        }

        /// <summary>
        /// Parses "WxH" or "WxH@Hz". hz is 0 when the key has no refresh rate.
        /// </summary>
        public static bool TryParseKey(string key, out int width, out int height, out int hz)
        {
            width = height = hz = 0;
            if (string.IsNullOrEmpty(key))
                return false;

            string size = key;
            int at = key.IndexOf('@');
            if (at >= 0)
            {
                size = key.Substring(0, at);
                if (!int.TryParse(key.Substring(at + 1), NumberStyles.None, CultureInfo.InvariantCulture, out hz) || hz <= 0)
                    return false;
            }

            int x = size.IndexOf('x');
            if (x <= 0)
                return false;

            return int.TryParse(size.Substring(0, x), NumberStyles.None, CultureInfo.InvariantCulture, out width)
                && int.TryParse(size.Substring(x + 1), NumberStyles.None, CultureInfo.InvariantCulture, out height)
                && width > 0 && height > 0;
        }

        /// <summary>
        /// Key with the nearest size (sum of width and height differences), then the nearest refresh rate.
        /// Returns null if the key cannot be parsed or nothing matches.
        /// </summary>
        public static string FindClosest(string key, IReadOnlyList<string> keys)
        {
            if (keys == null || !TryParseKey(key, out int targetWidth, out int targetHeight, out int targetHz))
                return null;

            string best = null;
            int bestSize = int.MaxValue;
            int bestHz = int.MaxValue;

            foreach (string candidate in keys)
            {
                if (!TryParseKey(candidate, out int width, out int height, out int hz))
                    continue;

                int sizeDiff = Math.Abs(width - targetWidth) + Math.Abs(height - targetHeight);
                int hzDiff = Math.Abs(hz - targetHz);
                if (sizeDiff < bestSize || (sizeDiff == bestSize && hzDiff < bestHz))
                {
                    best = candidate;
                    bestSize = sizeDiff;
                    bestHz = hzDiff;
                }
            }

            return best;
        }

        /// <summary>
        /// 143.998 Hz is shown and saved as 144.
        /// </summary>
        public static int RoundHz(RefreshRate rate) =>
            rate.denominator == 0 ? 0 : (int)Math.Round(rate.value);

        static int CompareLargestFirst(ResolutionOption a, ResolutionOption b)
        {
            int result = b.Resolution.width.CompareTo(a.Resolution.width);
            if (result != 0)
                return result;

            result = b.Resolution.height.CompareTo(a.Resolution.height);
            if (result != 0)
                return result;

            return b.Resolution.refreshRateRatio.value.CompareTo(a.Resolution.refreshRateRatio.value);
        }
    }
}