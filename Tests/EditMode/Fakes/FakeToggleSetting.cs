using System;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FakeToggleSetting : ToggleSetting
    {
        public int ApplyCount { get; private set; }
        public bool LastApplied { get; private set; }

        public bool Supported { get; set; } = true;
        public bool ThrowOnApply { get; set; }

        public override bool IsSupported(SettingsContext context) => Supported;

        public override void Apply(bool value, SettingsApplyContext context)
        {
            if (ThrowOnApply)
                throw new InvalidOperationException("Apply failed on purpose.");

            ApplyCount++;
            LastApplied = value;
        }
    }
}