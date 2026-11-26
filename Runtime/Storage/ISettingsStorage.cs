namespace MiradorWorks.Settings
{
    /// <summary>
    /// Where settings are persisted. Implement it to connect your own save system.
    /// </summary>
    public interface ISettingsStorage
    {
        /// <summary>
        /// Returns false when there is nothing saved or it cannot be read. Must not throw.
        /// </summary>
        bool TryLoad(out SettingsData data);

        /// <summary>
        /// Persists the data. May throw on IO errors: the settings service catches and logs them.
        /// </summary>
        void Save(SettingsData data);
    }
}