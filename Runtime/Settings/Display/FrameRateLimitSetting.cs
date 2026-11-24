using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Frame rate cap. Unity ignores it while VSync is on: disable it in your own UI in that case.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Display/Frame Rate Limit", fileName = "FrameRateLimitSetting")]
    public sealed class FrameRateLimitSetting : ChoiceSetting
    {
        public const string UnlimitedKey = "unlimited";

        [Tooltip("Offered caps in frames per second. Invalid or repeated values are ignored.")]
        [SerializeField] int[] rates = { 30, 60, 120, 144, 240 };
        [SerializeField] LocalizedString unlimitedLabel = new LocalizedString();

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context)
        {
            List<int> validRates = ValidRates();
            var options = new List<ChoiceOption>(validRates.Count + 1);

            foreach (int rate in validRates)
                options.Add(ChoiceOption.Text(rate.ToString(CultureInfo.InvariantCulture), $"{rate} FPS"));

            options.Add(ChoiceOption.Localized(UnlimitedKey, unlimitedLabel));
            return options;
        }

        /// <summary>
        /// The inspector's default key if it is an option; otherwise unlimited.
        /// </summary>
        public override string GetDefault(SettingsContext context) =>
            IndexOf(GetOptions(context), DefaultKey) >= 0 ? DefaultKey : UnlimitedKey;

        public override void Apply(string value, SettingsApplyContext context) =>
            Application.targetFrameRate = TryGetRate(value, out int rate) ? rate : -1;

        /// <summary>
        /// Frames per second of a key, or false for "unlimited" and invalid keys.
        /// </summary>
        public static bool TryGetRate(string key, out int rate) =>
            int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out rate) && rate > 0;

        List<int> ValidRates()
        {
            var valid = new List<int>();
            if (rates != null)
            {
                foreach (int rate in rates)
                {
                    if (rate > 0 && !valid.Contains(rate))
                        valid.Add(rate);
                }
            }

            valid.Sort();
            return valid;
        }
    }
}