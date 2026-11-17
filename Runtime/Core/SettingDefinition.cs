using System;
using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Describes a setting. It never stores the current value: that lives in the settings service.
    /// Inherit from <see cref="SettingDefinition{T}"/> (or ToggleSetting, RangeSetting, ChoiceSetting), not from this class.
    /// </summary>
    public abstract class SettingDefinition : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] LocalizedString label = new LocalizedString();
        [SerializeField] LocalizedString description = new LocalizedString();
        [SerializeField] SettingCategory category;
        [SerializeField] ApplyMode applyMode = ApplyMode.Immediate;
        [SerializeField] bool requiresConfirmation;

        // Only SettingDefinition<T> (same assembly) can inherit directly.
        private protected SettingDefinition() { }

        /// <summary>Stable persistence key. Never derived from the asset name./// </summary>
        public string Id => id;
        public LocalizedString Label => label;
        public LocalizedString Description => description;
        public SettingCategory Category => category;
        public ApplyMode ApplyMode => applyMode;
        public bool RequiresConfirmation => requiresConfirmation;

        public abstract Type ValueType { get; }

        /// <summary>Whether the setting can be used on this platfotm or configuration./// </summary>
        public virtual bool IsSupported(SettingsContext context) => true;
    }
}