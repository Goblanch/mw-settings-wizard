using System;

namespace MiradorWorks.Settings
{
    /// <summary>Keeps settings in memory only. Meant for tests and prototypes./// </summary>
    public sealed class InMemorySettingsStorage : ISettingsStorage
    {
        SettingsData stored;

        public InMemorySettingsStorage() { }

        public InMemorySettingsStorage(SettingsData initial)
        {
            stored = initial?.Clone();
        }

        public int SaveCount { get; private set; }

        public bool TryLoad(out SettingsData data)
        {
            data = stored?.Clone();
            return data != null;
        }

        public void Save(SettingsData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            stored = data.Clone();
            SaveCount++;
        }
    }
}