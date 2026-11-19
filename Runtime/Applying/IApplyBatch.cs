namespace MiradorWorks.Settings
{
    /// <summary>
    /// Collects changes from several settings that must reach the engine in a single call
    /// (e.g. resolution and display mode both go through Screen.SetResolution).
    /// Get one with <see cref="SettingsApplyContext.GetBatch(TBatch)"/> inside Apply;
    /// the service commits it once, after every setting of the pass has been applied.
    /// </summary>
    public interface IApplyBatch
    {
        void Commit(SettingsContext context);
    }
}