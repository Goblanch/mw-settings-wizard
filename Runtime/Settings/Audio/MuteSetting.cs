using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Silences one audio channel. Its volume comes back when unmuted
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Audio/Mute", fileName = "MuteSetting")]
    public sealed class MuteSetting : ToggleSetting, IAudioChannelSetting
    {
        [SerializeField] AudioChannel channel;

        public AudioChannel Channel => channel;

        public override bool IsSupported(SettingsContext context) => channel != null && channel.IsAvailable;

        public override void Apply(bool value, SettingsApplyContext context)
        {
            if (channel != null)
                AudioChannelStates.Get(context.Settings).SetMuted(channel, value);
        }
    }
}