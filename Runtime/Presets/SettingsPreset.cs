using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Named set of values for several settings (Low, High...). Values are written as each setting
    /// serializes them: "true", "0,75", "1920x1080@144"
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Preset", fileName = "SettingsPreset")]
    public sealed class SettingsPreset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] SettingDefinition setting;
            [SerializeField] string value;

            public Entry(SettingDefinition setting, string value)
            {
                this.setting = setting;
                this.value = value;
            }

            public SettingDefinition Setting => setting;
            public string Value => value;
        }

        [Tooltip("Saved as the preset selector's value. Letters, digits, '.', '_' or '-'.")]
        [SerializeField] string key;
        [SerializeField] LocalizedString label = new LocalizedString();
        [SerializeField] List<Entry> entries = new List<Entry>();

        public string Key => key;
        public LocalizedString Label => label;
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>
        /// Builds a preset at runtime (tests, tools). Presets are normally assets edited in the inspector.
        /// </summary>
        public static SettingsPreset Create(string key, IEnumerable<Entry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var preset = CreateInstance<SettingsPreset>();
            preset.name = key;
            preset.key = key;
            preset.entries = new List<Entry>(entries);
            return preset;
        }
    }
}