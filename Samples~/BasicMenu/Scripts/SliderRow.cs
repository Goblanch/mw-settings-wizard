using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiradorWorks.Settings.Samples
{
    public sealed class SliderRow : SettingRow
    {
        [SerializeField] Slider slider;
        [SerializeField] TMP_Text valueText;
        [SerializeField] string valueFormat = "0.00";

        RangeSetting Setting => (RangeSetting)Definition;

        protected override void OnBind()
        {
            slider.minValue = Setting.Min;
            slider.maxValue = Setting.Max;
            slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(OnMoved);
        }

        protected override void Refresh()
        {
            float value = Service.Get(Setting);
            slider.SetValueWithoutNotify(value);
            if (valueText != null)
                valueText.text = value.ToString(valueFormat, CultureInfo.CurrentCulture);
        }

        void OnMoved(float value)
        {
            Service.Set(Setting, value);
            Refresh();
        }
    }
}