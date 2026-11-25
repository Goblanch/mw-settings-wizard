using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MiradorWorks.Settings.Samples
{
    public sealed class DropdownRow : SettingRow
    {
        [SerializeField] TMP_Dropdown dropdown;

        readonly List<ChoiceOption> options = new List<ChoiceOption>();

        ChoiceSetting Setting => (ChoiceSetting)Definition;

        protected override void OnBind()
        {
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(OnPicked);

            if (LocalizationSettings.HasSettings)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
                LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            }

            RebuildOptions();
        }

        protected override void Refresh()
        {
            int index = ChoiceSetting.IndexOf(options, Service.Get(Setting));
            if (index < 0)
            {
                RebuildOptions();
                index = ChoiceSetting.IndexOf(options, Service.Get(Setting));
            }

            dropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        }

        protected override void OnDestroy()
        {
            if (LocalizationSettings.HasSettings)
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        void OnPicked(int index)
        {
            if (index >= 0 && index < options.Count)
                Service.Set(Setting, options[index].Key);

            Refresh();
        }

        void OnLocaleChanged(Locale locale)
        {
            RebuildOptions();
            Refresh();
        }

        void RebuildOptions()
        {
            options.Clear();
            options.AddRange(Setting.GetOptions(Service.Context));

            var labels = new List<string>(options.Count);
            foreach (ChoiceOption option in options)
                labels.Add(LabelOf(option));

            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
        }

        static string LabelOf(ChoiceOption option)
        {
            if (option.IsLocalized && !option.LocalizedLabel.IsEmpty)
                return option.LocalizedLabel.GetLocalizedString();

            return option.RawLabel ?? option.Key;
        }
    }
}