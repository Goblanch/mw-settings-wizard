using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Setting with a list of options. The value is the option key, never its index.
    /// Rendered as dropdown.
    /// </summary>
    public abstract class ChoiceSetting : SettingDefinition<string>
    {
        [Tooltip("Key of the default option. If empty or missing, the first option is used.")]
        [SerializeField] string defaultKey;

        /// <summary>The key set in the inspector, for subclasses with their own default logic./// </summary>
        protected string DefaultKey => defaultKey;

        /// <summary>Current options. May be generated at runtime; cache them if they are expensive to build. /// </summary>
        public abstract IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context);

        /// <summary>No options means nothing to choose: unsupported./// </summary>
        public override bool IsSupported(SettingsContext context)
        {
            IReadOnlyList<ChoiceOption> options = GetOptions(context);
            return options != null && options.Count > 0;
        }

        public override string GetDefault(SettingsContext context)
        {
            IReadOnlyList<ChoiceOption> options = GetOptions(context);
            if (IndexOf(options, defaultKey) >= 0)
                return defaultKey;

            return options != null && options.Count > 0 ? options[0].Key : null;
        }

        public override string Sanitize(string value, SettingsContext context)
        {
            IReadOnlyList<ChoiceOption> options = GetOptions(context);
            if (IndexOf(options, value) >= 0)
                return value;

            if (value != null)
            {
                string closest = FindClosest(value, options);
                if (IndexOf(options, closest) >= 0)
                    return closest;
            }

            return GetDefault(context);
        }

        /// <summary>Override to map a missing key to a similar option (e.g. the nearest resolution)./// </summary>
        protected virtual string FindClosest(string key, IReadOnlyList<ChoiceOption> options) => null;

        public override string Serialize(string value) => value ?? string.Empty;

        public override bool TryDeserialize(string raw, out string value)
        {
            value = raw;
            return !string.IsNullOrEmpty(raw);
        }

        public override bool AreEqual(string a, string b) => string.Equals(a, b, StringComparison.Ordinal);

        /// <summary>Index of the option with this key, or -1. Useful for dropdowns./// </summary>
        public static int IndexOf(IReadOnlyList<ChoiceOption> options, string key)
        {
            if (options == null || key == null)
                return -1;

            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].Key, key, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}