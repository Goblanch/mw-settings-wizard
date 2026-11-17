using UnityEngine;
using UnityEngine.Localization;

namespace MiradorWorks.Settings
{
    /// <summary>Groups settings in the UI (Display, Graphics...)./// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Category", fileName = "SettingsCategory")]
    public sealed class SettingCategory : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] LocalizedString label = new LocalizedString();
        [SerializeField] int order;

        public string Id => id;
        public LocalizedString Label => label;
        public int Order => order;
    }
}