using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Dropdown to pick a preset of the profile. Its value is not chosen but derived: the service shows
    /// the first preset whose values all match the current ones, or "custom" if none does.
    /// Setting it to a preset key applies that preset. Only one per profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Preset Selector", fileName = "PresetSetting")]
    public sealed class PresetSetting : ChoiceSetting
    {
        public const string CustomKey = "custom";

        [SerializeField] LocalizedString customLabel = new LocalizedString();

        public LocalizedString CustomLabel => customLabel;

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context)
        {
            var options = new List<ChoiceOption>();

            IReadOnlyList<SettingsPreset> presets = context.Profile != null ? context.Profile.Presets : null;
            if (presets != null)
            {
                foreach (SettingsPreset preset in presets)
                {
                    if (preset != null && !string.IsNullOrEmpty(preset.Key))
                        options.Add(ChoiceOption.Localized(preset.Key, preset.Label));
                }
            }

            options.Add(ChoiceOption.Localized(CustomKey, customLabel));
            return options;
        }

        // The service applies presets itself: applying here would overwrite the player's own values on startup.
        public override void Apply(string value, SettingsApplyContext context) { }
    }
}