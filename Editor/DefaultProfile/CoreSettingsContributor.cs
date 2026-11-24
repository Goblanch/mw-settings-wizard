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

            builder.AddSetting<DisplayModeSetting>("display.mode", display, ApplyMode.Deferred, requiresConfirmation: true);
            builder.AddSetting<ResolutionSetting>("display.resolution", display, ApplyMode.Deferred, requiresConfirmation: true);
            builder.AddSetting<VSyncSetting>("display.vsync", display,
                configure: so => so.FindProperty("defaultValue").boolValue = true);
            builder.AddSetting<FrameRateLimitSetting>("display.fps", display);

            var textures = builder.AddSetting<TextureQualitySetting>("graphics.textures", graphics);
            builder.AddPresetValue(DefaultProfileBuilder.Low, textures, TextureQualitySetting.Half);
            builder.AddPresetValue(DefaultProfileBuilder.Medium, textures, TextureQualitySetting.Full);
            builder.AddPresetValue(DefaultProfileBuilder.High, textures, TextureQualitySetting.Full);
        }
    }
}