namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FakeRangeSetting : RangeSetting
    {
        public int ApplyCount { get; private set; }
        public float LastApplied { get; private set; }

        public override void Apply(float value, SettingsApplyContext context)
        {
            ApplyCount++;
            LastApplied = value;
        }
    }
}