using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Borderless, exclusive fullscreen or windowed. Only the modes of the current platform are offered.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Display/Display Mode", fileName = "DisplayModeSetting")]
    public sealed class DisplayModeSetting : ChoiceSetting
    {
        [SerializeField] LocalizedString borderlessLabel = new LocalizedString();
        [SerializeField] LocalizedString exclusiveLabel = new LocalizedString();
        [SerializeField] LocalizedString windowedLabel = new LocalizedString();
        [Tooltip("macOS only")]
        [SerializeField] LocalizedString maximizedLabel = new LocalizedString();

        public override bool IsSupported(SettingsContext context) =>
            DisplayModes.IsDesktop(Application.platform) && base.IsSupported(context);

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context)
        {
            IReadOnlyList<string> keys = DisplayModes.KeysFor(Application.platform);
            var options = new List<ChoiceOption>(keys.Count);
            foreach (string key in keys)
                options.Add(ChoiceOption.Localized(key, LabelFor(key)));

            return options;
        }

        public override void Apply(string value, SettingsApplyContext context)
        {
            if (DisplayModes.TryGetMode(value, out FullScreenMode mode))
                context.GetBatch<DisplayBatch>().Mode = mode;
        }

        LocalizedString LabelFor(string key) => key switch
        {
            DisplayModes.Exclusive => exclusiveLabel,
            DisplayModes.Windowed => windowedLabel,
            DisplayModes.Maximized => maximizedLabel,
            _ => borderlessLabel
        };
    }
}