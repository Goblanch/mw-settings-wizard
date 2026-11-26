using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings.URP
{
    /// <summary>
    /// Multisample anti-aliasing: off, 2x, 4x or 8x. Smooths geometry edges at a GPU cost.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Graphics/MSAA (URP)", fileName = "MsaaSetting")]
    public sealed class MsaaSetting : ChoiceSetting
    {
        public const string Off = "off";
        public const string X2 = "2x";
        public const string X4 = "4x";
        public const string X8 = "8x";

        [SerializeField] LocalizedString offLabel = new LocalizedString();

        public override bool IsSupported(SettingsContext context) =>
            UrpAssetHandle.IsUrpActive && base.IsSupported(context);

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context) => new[]
        {
            ChoiceOption.Localized(Off, offLabel),
            ChoiceOption.Text(X2, "2×"),
            ChoiceOption.Text(X4, "4×"),
            ChoiceOption.Text(X8, "8×")
        };

        public override void Apply(string value, SettingsApplyContext context) =>
            UrpAssetHandle.Get(context.Settings).Asset.msaaSampleCount = SampleCountFor(value);

        /// <summary>
        /// URP sample count: 1 means MSAA off.
        /// </summary>
        public static int SampleCountFor(string key) => key switch
        {
            X2 => 2,
            X4 => 4,
            X8 => 8,
            _ => 1
        };
    }
}