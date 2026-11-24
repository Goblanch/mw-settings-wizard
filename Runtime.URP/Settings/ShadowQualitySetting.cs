using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UrpShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;

namespace MiradorWorks.Settings.URP
{
    /// <summary>
    /// One shadow quality level: what a single dropdown option sets on the URP asset.
    /// </summary>
    [Serializable]
    public struct ShadowTier
    {
        [SerializeField] string key;
        [SerializeField] LocalizedString label;
        [Tooltip("Maximum distance at which shadows are drawn. 0 turns them off.")]
        [Min(0f)]
        [SerializeField] float distance;
        [Range(1, 4)]
        [SerializeField] int cascades;
        [SerializeField] UrpShadowResolution mainLightResolution;
        [SerializeField] UrpShadowResolution additionalLightsResolution;

        public ShadowTier(string key, float distance, int cascades,
            UrpShadowResolution mainLightResolution, UrpShadowResolution additionalLightsResolution)
        {
            this.key = key;
            label = new LocalizedString();
            this.distance = distance;
            this.cascades = cascades;
            this.mainLightResolution = mainLightResolution;
            this.additionalLightsResolution = additionalLightsResolution;
        }

        public string Key => key;
        public LocalizedString Label => label;
        public float Distance => distance;
        public int Cascades => cascades;
        public UrpShadowResolution MainLightResolution => mainLightResolution;
        public UrpShadowResolution AdditionalLightsResolution => additionalLightsResolution;
    }

    /// <summary>
    /// A single dropdown for shadows: each level sets distance, cascades and shadow map resolutions.
    /// The project's URP Asset must have shadows enabled, or builds strip the shaders they need.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Graphics/Shadow Quality (URP)", fileName = "ShadowQualitySetting")]
    public sealed class ShadowQualitySetting : ChoiceSetting
    {
        [Tooltip("Ordered from lowest to highest. Without a default key, the last level is the default")]
        [SerializeField]
        ShadowTier[] tiers =
        {
            new ShadowTier("off", 0f, 1, UrpShadowResolution._256, UrpShadowResolution._256),
            new ShadowTier("low", 25f, 1, UrpShadowResolution._1024, UrpShadowResolution._512),
            new ShadowTier("medium", 50f, 2, UrpShadowResolution._2048, UrpShadowResolution._1024),
            new ShadowTier("high", 100f, 4, UrpShadowResolution._4096, UrpShadowResolution._2048)
        };

        public IReadOnlyList<ShadowTier> Tiers => tiers;

        public override bool IsSupported(SettingsContext context) =>
            UrpAssetHandle.IsUrpActive && base.IsSupported(context);

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context)
        {
            List<ShadowTier> valid = ValidTiers();
            var options = new List<ChoiceOption>(valid.Count);
            foreach (ShadowTier tier in valid)
                options.Add(ChoiceOption.Localized(tier.Key, tier.Label ?? new LocalizedString()));

            return options;
        }

        public override string GetDefault(SettingsContext context)
        {
            IReadOnlyList<ChoiceOption> options = GetOptions(context);
            if (IndexOf(options, DefaultKey) >= 0)
                return DefaultKey;

            return options.Count > 0 ? options[options.Count - 1].Key : null;
        }

        public override void Apply(string value, SettingsApplyContext context)
        {
            if (!TryGetTier(value, out ShadowTier tier))
                return;

            var asset = UrpAssetHandle.Get(context.Settings).Asset;
            asset.shadowDistance = tier.Distance;
            asset.shadowCascadeCount = Mathf.Clamp(tier.Cascades, 1, 4);
            asset.mainLightShadowmapResolution = (int)tier.MainLightResolution;
            asset.additionalLightsShadowmapResolution = (int)tier.AdditionalLightsResolution;
        }

        bool TryGetTier(string key, out ShadowTier tier)
        {
            foreach (ShadowTier candidate in ValidTiers())
            {
                if (string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    tier = candidate;
                    return true;
                }
            }

            tier = default;
            return false;
        }

        // Levels with an empty or repeated key are skipped: they could never be saved unambiguously
        List<ShadowTier> ValidTiers()
        {
            var valid = new List<ShadowTier>();
            var keys = new HashSet<string>(StringComparer.Ordinal);

            if (tiers != null)
            {
                foreach (ShadowTier tier in tiers)
                {
                    if (!string.IsNullOrEmpty(tier.Key) && keys.Add(tier.Key))
                        valid.Add(tier);
                }
            }

            return valid;
        }
    }
}