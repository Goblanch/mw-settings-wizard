using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Vertical sync. While it is on, Unity ignores the frame rate limit.
    /// Unity only uses it in players: the Editor ignores it.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Display/VSync", fileName = "VSyncSetting")]
    public sealed class VSyncSetting : ToggleSetting
    {
        public override void Apply(bool value, SettingsApplyContext context) =>
            QualitySettings.vSyncCount = value ? 1 : 0;
    }
}