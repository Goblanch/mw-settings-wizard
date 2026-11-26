using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>On/off setting. Rendered as toggle./// </summary>
    public abstract class ToggleSetting : SettingDefinition<bool>
    {
        [SerializeField] bool defaultValue;

        public override bool GetDefault(SettingsContext context) => defaultValue;

        public override string Serialize(bool value) => value ? "true" : "false";

        public override bool TryDeserialize(string raw, out bool value) => bool.TryParse(raw, out value);
    }
}