using System;
using TMPro;
using UnityEngine;

namespace MiradorWorks.Settings.Samples
{
    /// <summary>
    /// Base of the menu rows: one per base setting type (toggle, slider, dropdown).
    /// It shows the setting's label and refreshes the control whenever the service reports a change.
    /// </summary>
    public abstract class SettingRow : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        readonly LocalizedLabelBinding labelBinding = new LocalizedLabelBinding();

        protected SettingsService Service { get; private set; }
        public SettingDefinition Definition { get; private set; }

        public void Bind(SettingsService service, SettingDefinition setting)
        {
            Unbind();

            Service = service ?? throw new ArgumentNullException(nameof(service));
            Definition = setting != null ? setting : throw new ArgumentNullException(nameof(setting));

            labelBinding.Bind(setting.Label, setting.name, label);
            Service.ValueChanged += OnValueChanged;

            OnBind();
            Refresh();
        }

        /// <summary>Configure the control and its listeners. Called once per Bind./// </summary>
        protected abstract void OnBind();

        /// <summary>Show the service's current value without raising the control's own events./// </summary>
        protected abstract void Refresh();

        protected virtual void OnDestroy() => Unbind();

        void OnValueChanged(SettingDefinition changed)
        {
            if (changed == Definition)
                Refresh();
        }

        void Unbind()
        {
            labelBinding.Dispose();
            if (Service != null)
                Service.ValueChanged -= OnValueChanged;

            Service = null;
            Definition = null;
        }
    }
}