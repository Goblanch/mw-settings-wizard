using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class InMemorySettingsStorageTests
    {
        [Test]
        public void TryLoad_NothingSaved_ReturnsFalse()
        {
            var storage = new InMemorySettingsStorage();

            Assert.That(storage.TryLoad(out SettingsData data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void Save_ThenTryLoad_ReturnsSavedValues()
        {
            var storage = new InMemorySettingsStorage();
            var data = new SettingsData();
            data.Set("a", "1");

            storage.Save(data);

            Assert.That(storage.TryLoad(out SettingsData loaded), Is.True);
            Assert.That(loaded.TryGetValue("a", out string value), Is.True);
            Assert.That(value, Is.EqualTo("1"));
            Assert.That(storage.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void StoredData_IsNotAffectedByLaterChanges()
        {
            var storage = new InMemorySettingsStorage();
            var data = new SettingsData();
            data.Set("a", "1");
            storage.Save(data);

            data.Set("a", "changed-after-save");
            storage.TryLoad(out SettingsData loaded);
            loaded.Set("a", "changed-after-load");

            storage.TryLoad(out SettingsData again);
            Assert.That(again.TryGetValue("a", out string value), Is.True);
            Assert.That(value, Is.EqualTo("1"));
        }
    }
}