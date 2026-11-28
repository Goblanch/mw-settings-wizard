using UnityEditor;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// The settings of the core package, with the ids and modes suggested in the design document.
    /// </summary>
    internal sealed class CoreSettingsContributor : DefaultProfileContributor
    {
        public override int Order => -100;

        public override void Contribute(DefaultProfileBuilder builder)
        {
            SettingCategory display = builder.Category(DefaultProfileBuilder.DisplayCategory, 0);
            SettingCategory graphics = builder.Category(DefaultProfileBuilder.GraphicsCategory, 1);
            SettingCategory audio = builder.Category(DefaultProfileBuilder.AudioCategory, 2);

            builder.AddSetting<DisplayModeSetting>("display.mode", display, ApplyMode.Deferred, requiresConfirmation: true);
            builder.AddSetting<ResolutionSetting>("display.resolution", display, ApplyMode.Deferred, requiresConfirmation: true);
            builder.AddSetting<VSyncSetting>("display.vsync", display,
                configure: so => so.FindProperty("defaultValue").boolValue = true);
            builder.AddSetting<FrameRateLimitSetting>("display.fps", display);

            var textures = builder.AddSetting<TextureQualitySetting>("graphics.textures", graphics);
            builder.AddPresetValue(DefaultProfileBuilder.Low, textures, TextureQualitySetting.Half);
            builder.AddPresetValue(DefaultProfileBuilder.Medium, textures, TextureQualitySetting.Full);
            builder.AddPresetValue(DefaultProfileBuilder.High, textures, TextureQualitySetting.Full);

            // A listener channel works without an AudioMixer. Games with a mixer swap it for a Mixer Parameter Channel.
            var master = builder.CreateAsset<ListenerChannel>("Master Channel");
            builder.AddSetting<VolumeSetting>("audio.master", audio,
                configure: so => so.FindProperty("channel").objectReferenceValue = master);
            builder.AddSetting<MuteSetting>("audio.mute", audio,
                configure: so => so.FindProperty("channel").objectReferenceValue = master);
        }
    }
}