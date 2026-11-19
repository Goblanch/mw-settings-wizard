using System.IO;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class ThrowingSaveStorage : ISettingsStorage
    {
        public bool TryLoad(out SettingsData data)
        {
            data = null;
            return false;
        }

        public void Save(SettingsData data) => throw new IOException("Disk full.");
    }
}