using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Resolutions of the current display. By default it picks the display's native resolution;
    /// a saved resolution that no longer exists (another monitor) falls back to the closest one.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Display/Resolution", fileName = "ResolutionSetting")]
    public sealed class ResolutionSetting : ChoiceSetting
    {
        [Tooltip("Smaller resolutions are hidden, unless the display has none this tall.")]
        [SerializeField] int minimumHeight = 720;
        [Tooltip("List each refresh rate separately. If off, each size uses its highest refresh rate.")]
        [SerializeField] bool includeRefreshRate = true;

        public override bool IsSupported(SettingsContext context) =>
            DisplayModes.IsDesktop(Application.platform) && base.IsSupported(context);

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context)
        {
            List<ResolutionOption> built = BuildOptions();
            var options = new List<ChoiceOption>(built.Count);
            foreach (ResolutionOption option in built)
                options.Add(ChoiceOption.Text(option.Key, option.Label));

            return options;
        }

        /// <summary>
        /// The inspector's default key if the display has it; otherwise the native resolution or the closest.
        /// </summary>
        public override string GetDefault(SettingsContext context)
        {
            List<ResolutionOption> options = BuildOptions();
            if (options.Count == 0)
                return null;

            if (Contains(options, DefaultKey))
                return DefaultKey;

            DisplayInfo display = Screen.mainWindowDisplayInfo;
            string nativeKey = ResolutionOptions.GetKey(
                display.width, display.height, ResolutionOptions.RoundHz(display.refreshRate), includeRefreshRate);

            if (Contains(options, nativeKey))
                return nativeKey;

            return ResolutionOptions.FindClosest(nativeKey, KeysOf(options)) ?? options[0].Key;
        }

        protected override string FindClosest(string key, IReadOnlyList<ChoiceOption> options)
        {
            var keys = new List<string>(options.Count);
            foreach (ChoiceOption option in options)
                keys.Add(option.Key);

            return ResolutionOptions.FindClosest(key, keys);
        }

        public override void Apply(string value, SettingsApplyContext context)
        {
            foreach (ResolutionOption option in BuildOptions())
            {
                if (string.Equals(option.Key, value, StringComparison.Ordinal))
                {
                    context.GetBatch<DisplayBatch>().Resolution = option.Resolution;
                    return;
                }
            }
        }

        List<ResolutionOption> BuildOptions() =>
            ResolutionOptions.Build(Screen.resolutions, minimumHeight, includeRefreshRate);

        static bool Contains(List<ResolutionOption> options, string key)
        {
            if (key == null)
                return false;

            foreach (ResolutionOption option in options)
            {
                if (string.Equals(option.Key, key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static List<string> KeysOf(List<ResolutionOption> options)
        {
            var keys = new List<string>(options.Count);
            foreach (ResolutionOption option in options)
                keys.Add(option.Key);

            return keys;
        }
    }
}