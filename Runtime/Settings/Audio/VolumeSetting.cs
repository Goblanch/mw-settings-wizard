using UnityEngine;

namespace MiradorWorks.Settings
{
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Audio/Volume", fileName = "VolumeSetting")]
    public sealed class VolumeSetting : RangeSetting, IAudioChannelSetting
    {
        [SerializeField] AudioChannel channel;

        public VolumeSetting() : base(min: 0f, max: 1f, step: 0.05f, defaultValue: 1f) { }

        public AudioChannel Channel => channel;

        public override bool IsSupported(SettingsContext context) => channel != null && channel.IsAvailable;

        public override void Apply(float value, SettingsApplyContext context)
        {
            if (channel != null)
                AudioChannelStates.Get(context.Settings).SetVolume(channel, value);
        }
    }
}