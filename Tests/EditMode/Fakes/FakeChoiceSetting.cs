using System.Collections.Generic;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FakeChoiceSetting : ChoiceSetting
    {
        public List<ChoiceOption> Options { get; } = new List<ChoiceOption>();

        /// <summary>Value returned by FindClosest./// </summary>
        public string Closest { get; set; }

        public int ApplyCount { get; private set; }
        public string LastApplied { get; private set; }

        public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context) => Options;

        protected override string FindClosest(string key, IReadOnlyList<ChoiceOption> options) => Closest;

        public override void Apply(string value, SettingsApplyContext context)
        {
            ApplyCount++;
            LastApplied = value;
        }
    }
}