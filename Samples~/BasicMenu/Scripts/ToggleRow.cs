using UnityEngine;
using UnityEngine.UI;

namespace MiradorWorks.Settings.Samples
{
    public sealed class ToggleRow : SettingRow
    {
        [SerializeField] Toggle toggle;

        ToggleSetting Setting => (ToggleSetting)Definition;

        protected override void OnBind()
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(OnToggled);
        }

        protected override void Refresh() => toggle.SetIsOnWithoutNotify(Service.Get(Setting));

        void OnToggled(bool value)
        {
            Service.Set(Setting, value);
            Refresh();
        }
    }
}