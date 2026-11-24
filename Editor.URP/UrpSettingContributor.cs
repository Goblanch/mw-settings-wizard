using MiradorWorks.Settings.Editor;

namespace MiradorWorks.Settings.URP.Editor
{
    /// <summary>Adds the URP settings and their preset values to the default profile.</summary>
    internal sealed class UrpSettingsContributor : DefaultProfileContributor
    {
        public override void Contribute(DefaultProfileBuilder builder)
        {
            SettingCategory graphics = builder.Category(DefaultProfileBuilder.GraphicsCategory, 1);

            builder.AddSetting<RenderScaleSetting>("graphics.renderscale", graphics);

            var shadows = builder.AddSetting<ShadowQualitySetting>("graphics.shadows", graphics);
            builder.AddPresetValue(DefaultProfileBuilder.Low, shadows, "low");
            builder.AddPresetValue(DefaultProfileBuilder.Medium, shadows, "medium");
            builder.AddPresetValue(DefaultProfileBuilder.High, shadows, "high");

            var msaa = builder.AddSetting<MsaaSetting>("graphics.msaa", graphics);
            builder.AddPresetValue(DefaultProfileBuilder.Low, msaa, MsaaSetting.Off);
            builder.AddPresetValue(DefaultProfileBuilder.Medium, msaa, MsaaSetting.X2);
            builder.AddPresetValue(DefaultProfileBuilder.High, msaa, MsaaSetting.X4);
        }
    }
}