using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Global volume through AudioListener.volume. For games without an AudioMixer.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Audio/Listener Channel", fileName = "ListenerChannel")]
    public sealed class ListenerChannel : AudioChannel
    {
        protected override void ApplyVolume(float linear01) => AudioListener.volume = linear01;

        // Unity's default
        protected override void RestoreVolume() => AudioListener.volume = 1f;
    }
}