using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// The settings a game uses. Only the settings listed here exists for the service and the UI.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Profile", fileName = "SettingsProfile")]
    public sealed class SettingsProfile : ScriptableObject
    {
        [SerializeField] List<SettingDefinition> settings = new List<SettingDefinition>();
        [SerializeField] List<SettingsPreset> presets = new List<SettingsPreset>();

        public IReadOnlyList<SettingDefinition> Settings => settings;
        public IReadOnlyList<SettingsPreset> Presets => presets;

        /// <summary>
        /// Builds a profile at runtime (tests, tools). Profiles are normally assets edited in inspector
        /// </summary>
        public static SettingsProfile Create(IEnumerable<SettingDefinition> settings, IEnumerable<SettingsPreset> presets = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var profile = CreateInstance<SettingsProfile>();
            profile.name = "RuntimeSettingsProfile";
            profile.settings = new List<SettingDefinition>(settings);
            profile.presets = presets != null ? new List<SettingsPreset>(presets) : new List<SettingsPreset>();
            return profile;
        }

        public bool TryGet(string id, out SettingDefinition setting)
        {
            if (id != null)
            {
                foreach (SettingDefinition candidate in settings)
                {
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        setting = candidate;
                        return true;
                    }
                }
            }

            setting = null;
            return false;
        }

        /// <summary>
        /// Returns every prtoblem found, or an empty list if the profile is valid.
        /// The setting service refuses to initialize with an invalid profile.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            var seenAssets = new HashSet<SettingDefinition>();
            var assetsById = new Dictionary<string, SettingDefinition>(StringComparer.Ordinal);
            int presetSelectors = 0;

            for (int i = 0; i < settings.Count; i++)
            {
                SettingDefinition setting = settings[i];

                // Unity's == also catches destroyed or missing asset references.
                if (setting == null)
                {
                    errors.Add($"Setting at index {i} is empty or missing.");
                    continue;
                }

                if (!seenAssets.Add(setting))
                {
                    errors.Add($"'{setting.name}' is listed more than once.");
                    continue;
                }

                if (setting is PresetSetting)
                    presetSelectors++; ;

                if (!SettingId.IsValid(setting.Id))
                {
                    errors.Add(string.IsNullOrEmpty(setting.Id)
                    ? $"'{setting.name}' has no id."
                    : $"'{setting.name}' has an invalid id '{setting.Id}'. Use up to {SettingId.MaxLength} letters, digits, '.', '_' or '-'.");
                    continue;
                }

                if (assetsById.TryGetValue(setting.Id, out SettingDefinition other))
                {
                    errors.Add($"Duplicate id '{setting.Id}' in '{other.name}' and '{setting.name}'.");
                    continue;
                }

                assetsById.Add(setting.Id, setting);
            }

            if (presetSelectors > 1)
                errors.Add("Only one PresetSetting is allowed per profile.");

            ValidatePresets(errors, seenAssets);

            return errors;
        }

        void ValidatePresets(List<string> errors, HashSet<SettingDefinition> profileSettings)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < presets.Count; i++)
            {
                SettingsPreset preset = presets[i];
                if (preset == null)
                {
                    errors.Add($"Preset at index {i} is empty or missing.");
                    continue;
                }

                if (!SettingId.IsValid(preset.Key))
                    errors.Add($"Preset '{preset.name}' has an invalid key '{preset.Key}'.");
                else if (preset.Key == PresetSetting.CustomKey)
                    errors.Add($"Preset '{preset.name}' uses the reserved key '{PresetSetting.CustomKey}'.");
                else if (!keys.Add(preset.Key))
                    errors.Add($"Duplicate preset key '{preset.Key}' in '{preset.name}'.");

                var settingsInPreset = new HashSet<SettingDefinition>();
                foreach (SettingsPreset.Entry entry in preset.Entries)
                {
                    SettingDefinition setting = entry.Setting;

                    if (setting == null)
                        errors.Add($"Preset '{preset.name}' has an empty setting entry.");
                    else if (!profileSettings.Contains(setting))
                        errors.Add($"Preset '{preset.name}' uses '{setting.name}', which is not in the profile.");
                    else if (setting is PresetSetting)
                        errors.Add($"Preset '{preset.name}' cannot set the preset selector.");
                    else if (!settingsInPreset.Add(setting))
                        errors.Add($"Preset '{preset.name}' sets '{setting.name}' more than once.");
                    else if (!setting.CanDeserialize(entry.Value))
                        errors.Add($"Preset '{preset.name}' has an invalid value '{entry.Value}' for '{setting.name}'.");
                }
            }
        }
    }
}