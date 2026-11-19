using System;
using System.Collections.Generic;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class RecordingBatch : IApplyBatch
    {
        public List<string> Items { get; } = new List<string>();
        public int CommitCount { get; private set; }
        public bool ThrowOnCommit { get; set; }

        public void Commit(SettingsContext context)
        {
            CommitCount++;
            if (ThrowOnCommit)
                throw new InvalidOperationException("Batch failed on purpose.");
        }
    }
}