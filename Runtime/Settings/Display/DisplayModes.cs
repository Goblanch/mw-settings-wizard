using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Display mode keys and which ones each desktop platform supports.
    /// </summary>
    internal static class DisplayModes
    {
        public const string Borderless = "borderless";
        public const string Exclusive = "exclusive";
        public const string Windowed = "windowed";
        public const string Maximized = "maximized";

        static readonly string[] WindowsKeys = { Borderless, Exclusive, Windowed };
        static readonly string[] MacKeys = { Borderless, Maximized, Windowed };
        static readonly string[] LinuxKeys = { Borderless, Windowed };

        public static bool IsDesktop(RuntimePlatform platform) =>
            platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                     or RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                     or RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor;

        /// <summary>
        /// Exclusive fullscreen only exists on Windows and maxiized only on macOS.
        /// Borderless comes first so it is the default when the inspector sets none.
        /// </summary>
        public static IReadOnlyList<string> KeysFor(RuntimePlatform platform) => platform switch
        {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => WindowsKeys,
            RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => MacKeys,
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor => LinuxKeys,
            _ => Array.Empty<string>()
        };

        public static bool TryGetMode(string key, out FullScreenMode mode)
        {
            switch (key)
            {
                case Borderless: mode = FullScreenMode.FullScreenWindow; return true;
                case Exclusive: mode = FullScreenMode.ExclusiveFullScreen; return true;
                case Windowed: mode = FullScreenMode.Windowed; return true;
                case Maximized: mode = FullScreenMode.MaximizedWindow; return true;
                default: mode = default; return false;
            }
        }
    }
}