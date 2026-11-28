using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Converts the linear 0-1 volumne the player sees into the decibels an AudioMixer uses.
    /// </summary>
    internal static class AudioVolume
    {
        public const float SileceDecibels = -80f;

        // 20 * log10(0.0001) = -80 dB: anything quieter is silence.
        const float MinLinear = 0.0001f;

        public static float ToDecibels(float linear01)
        {
            if (float.IsNaN(linear01) || linear01 <= MinLinear)
                return SileceDecibels;

            return Mathf.Log10(Mathf.Min(linear01, 1f)) * 20f;
        }
    }
}