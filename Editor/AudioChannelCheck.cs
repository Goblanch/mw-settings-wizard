using System.Collections.Generic;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// Audio settings without a working channel are hidden from the menu and never applied.
    /// </summary>
    internal sealed class AudioChannelCheck : SettingsProfileCheck
    {
        public override void Collect(SettingsProfile profile, List<string> warnings)
        {
            foreach (SettingDefinition setting in profile.Settings)
            {
                if (!(setting is IAudioChannelSetting audioSetting))
                    continue;

                AudioChannel channel = audioSetting.Channel;

                if (channel == null)
                {
                    warnings.Add($"'{setting.name}' has no Audio Channel assigned: it is hidden from the menu and never applied.");
                }
                else if (!channel.IsAvailable)
                {
                    warnings.Add(
                        $"Audio Channel '{channel.name}' used by '{setting.name}' is not set up. " +
                        "For a Mixer Parameter Channel, assign the mixer and the exposed parameter.");
                }
            }
        }
    }
}