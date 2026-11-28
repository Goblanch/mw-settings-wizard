using System.Collections.Generic;

namespace MiradorWorks.Settings.Tests.EditMode
{
    /// <summary>Records the volumes it receives instead of touching the engine.</summary>
    public sealed class FakeAudioChannel : AudioChannel
    {
        public readonly List<float> Applied = new List<float>();
        public bool Available = true;

        public int RestoreCount { get; private set; }
        public float LastVolume => Applied[Applied.Count - 1];

        public override bool IsAvailable => Available;

        protected override void ApplyVolume(float linear01) => Applied.Add(linear01);

        protected override void RestoreVolume() => RestoreCount++;
    }
}