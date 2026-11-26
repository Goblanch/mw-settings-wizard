using UnityEngine;

namespace MiradorWorks.Settings.URP
{
    /// <summary>
    /// Internal rendering resolution relative to the screen: below 1 renders fewer pixels (faster)
    /// above 1 supersamples (sharper). The UI staus at native resolution.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Graphics/Render Scale (URP)", fileName = "RenderScaleSetting")]
    public sealed class RenderScaleSetting : RangeSetting
    {
        public RenderScaleSetting() : base(min: 0.5f, max: 2f, step: 0.05f, defaultValue: 1f) { }

        public override bool IsSupported(SettingsContext context) =>
            UrpAssetHandle.IsUrpActive && base.IsSupported(context);

        public override void Apply(float value, SettingsApplyContext context) =>
            UrpAssetHandle.Get(context.Settings).Asset.renderScale = value;
    }
}