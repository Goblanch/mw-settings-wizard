using System;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Collects display mode and resolution so both reach the engine in a single Screen.SetResolution call.
    /// A value that no setting provided in this pass keeps what the screen currently has.
    /// </summary>
    public sealed class DisplayBatch : IApplyBatch
    {
        public FullScreenMode? Mode { get; set; }
        public Resolution? Resolution { get; set; }

        public void Commit(SettingsContext context)
        {
            if (Mode == null && Resolution == null)
                return;

            FullScreenMode mode = Mode ?? Screen.fullScreenMode;

            int width;
            int height;
            RefreshRate refreshRate;
            if (Resolution.HasValue)
            {
                Resolution target = Resolution.Value;
                width = target.width;
                height = target.height;
                refreshRate = target.refreshRateRatio;
            }
            else
            {
                width = Screen.width;
                height = Screen.height;
                refreshRate = Screen.currentResolution.refreshRateRatio;
            }

            // Re-applying what the screen already has can still flicker (e.g. on startup): skip it
            if (IsAlreadyApplied(mode, width, height, refreshRate))
                return;

            Screen.SetResolution(width, height, mode, refreshRate);
        }

        static bool IsAlreadyApplied(FullScreenMode mode, int width, int height, RefreshRate refreshRate)
        {
            if (Screen.fullScreenMode != mode || Screen.width != width || Screen.height != height)
                return false;

            // The refresh rato only matters in exclusive fullscreen.
            return mode != FullScreenMode.ExclusiveFullScreen
                || Math.Abs(Screen.currentResolution.refreshRateRatio.value - refreshRate.value) < 0.5;
        }
    }
}