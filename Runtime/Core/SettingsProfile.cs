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

        public IReadOnlyList<SettingDefinition> Settings => settings;

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

            return errors;
        }
    }
}