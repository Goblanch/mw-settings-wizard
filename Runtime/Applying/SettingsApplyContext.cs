using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// One apply pass: every setting applied together (Initialize, ApplyPending, a reset or a single Set)
    /// shares the same context and therefore the same batches.
    /// </summary>
    public sealed class SettingsApplyContext
    {
        readonly List<IApplyBatch> batches = new List<IApplyBatch>();
        readonly Dictionary<Type, IApplyBatch> batchesByType = new Dictionary<Type, IApplyBatch>();
        bool committed;

        internal SettingsApplyContext(SettingsContext settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public SettingsContext Settings { get; }

        /// <summary>
        /// Returns this pass's batch of that type, creating it on first use.
        /// </summary>
        public TBatch GetBatch<TBatch>() where TBatch : class, IApplyBatch, new()
        {
            if (committed)
                throw new InvalidOperationException("This apply pass has already been committed. Do not keep the context after Apply.");

            if (batchesByType.TryGetValue(typeof(TBatch), out IApplyBatch existing))
                return (TBatch)existing;

            var batch = new TBatch();
            batchesByType.Add(typeof(TBatch), batch);
            batches.Add(batch);
            return batch;
        }

        internal void CommitBatches()
        {
            committed = true;

            foreach (IApplyBatch batch in batches)
            {
                try
                {
                    batch.Commit(Settings);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiradorWorks.Settings] Committing {batch.GetType().Name} failed.\n{e}");
                }
            }
        }
    }
}