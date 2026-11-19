namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SecondRecordingBatch : IApplyBatch
    {
        public int CommitCount { get; private set; }

        public void Commit(SettingsContext context) => CommitCount++;
    }
}