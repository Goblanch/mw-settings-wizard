using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Texture resolution through the global mipmap limit: full, half, quarter or eighth.
    /// Textures marked "Ignore Mipmap Limit" in their import settings are not affected.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Graphics/Texture Quality", fileName = "TextureQualitySetting")]
    public sealed class TextureQualitySetting : ChoiceSetting
    {
        public const string Full = "full";
        public const string Half = "half";
        public const string Quarter = "quarter";
        public const string Eighth = "eighth";

        [SerializeField] LocalizedString fullLabel = new LocalizedString();
        [SerializeField] LocalizedString halfLabel = new LocalizedString();
        [SerializeField] LocalizedString quarterLabel = new LocalizedString();
        [SerializeField] LocalizedString eighthLabel = new LocalizedString();

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context) => new[]
        {
            ChoiceOption.Localized(Full, fullLabel),
            ChoiceOption.Localized(Half, halfLabel),
            ChoiceOption.Localized(Quarter, quarterLabel),
            ChoiceOption.Localized(Eighth, eighthLabel)
        };

        public override void Apply(string value, SettingsApplyContext context) =>
            QualitySettings.globalTextureMipmapLimit = MipmapLimitFor(value);

        /// <summary>
        /// 0 = full resolution, each step halves it.
        /// </summary>
        public static int MipmapLimitFor(string key) => key switch
        {
            Half => 1,
            Quarter => 2,
            Eighth => 3,
            _ => 0
        };
    }
}