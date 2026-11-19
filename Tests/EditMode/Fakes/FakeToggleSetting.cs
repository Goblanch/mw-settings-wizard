using System;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FakeToggleSetting : ToggleSetting
    {
        public int ApplyCount { get; private set; }
        public bool LastApplied { get; private set; }

        public bool Supported { get; set; } = true;
        public bool ThrowOnApply { get; set; }

        /// <summary>Replaces the serialized default when set.</summary>
        public Func<SettingsContext, bool> DefaultProvider { get; set; }

        /// <summary>Runs inside Apply, after counting the call.</summary>
        public Action<bool, SettingsApplyContext> OnApply { get; set; }

        public override bool IsSupported(SettingsContext context) => Supported;

        public override bool GetDefault(SettingsContext context) =>
            DefaultProvider != null ? DefaultProvider(context) : base.GetDefault(context);

        public override void Apply(bool value, SettingsApplyContext context)
        {
            if (ThrowOnApply)
                throw new InvalidOperationException("Apply failed on purpose.");

            ApplyCount++;
            LastApplied = value;
            OnApply?.Invoke(value, context);
        }
    }
}