namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FakeToggleSetting : ToggleSetting
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