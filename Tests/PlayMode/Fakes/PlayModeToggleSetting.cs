namespace MiradorWorks.Settings.Tests.PlayMode
{
    public sealed class PlayModeToggleSetting : ToggleSetting
    {
        public int ApplyCount { get; private set; }
        public bool LastApplied { get; private set; }

        public override void Apply(bool value, SettingsApplyContext context)
        {
            ApplyCount++;
            LastApplied = value;
        }
    }
}