using System;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>One option of a <see cref="ChoiceSetting"/>. The key is what gets saved./// </summary>
    public readonly struct ChoiceOption
    {
        ChoiceOption(string key, LocalizedString localizedLabel, string rawLabel)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("Option cannot be null or empty.", nameof(key));

            Key = key;
            LocalizedLabel = localizedLabel;
            RawLabel = rawLabel;
        }

        public string Key { get; }

        /// <summary>Label for static options that must be translated./// </summary>
        public LocalizedString LocalizedLabel { get; }

        /// <summary>Label for generated options that do not need translation ("1920 x 1080 @ 144 Hz")./// </summary>
        public string RawLabel { get; }

        public bool IsLocalized => LocalizedLabel != null;

        public static ChoiceOption Localized(string key, LocalizedString label) =>
            new ChoiceOption(key, label ?? throw new ArgumentNullException(nameof(label)), null);

        public static ChoiceOption Text(string key, string label) =>
            new ChoiceOption(key, null, label ?? key);
    }
}