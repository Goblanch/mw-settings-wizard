using System;
using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SettingsDataTests
    {
        [Test]
        public void NewData_HasCurrentVersionAndNoEntries()
        {
            var data = new SettingsData();

            Assert.That(data.Version, Is.EqualTo(SettingsData.CurrentVersion));
            Assert.That(data.Count, Is.Zero);
        }

        [Test]
        public void Set_ThenTryGetValue_ReturnsValue()
        {
            var data = new SettingsData();
            data.Set("graphics.vsync", "true");

            Assert.That(data.TryGetValue("graphics.vsync", out string value), Is.True);
            Assert.That(value, Is.EqualTo("true"));
        }

        [Test]
        public void Set_ExistingId_ReplacesValue()
        {
            var data = new SettingsData();
            data.Set("a", "1");
            data.Set("a", "2");

            Assert.That(data.Count, Is.EqualTo(1));
            Assert.That(data.TryGetValue("a", out string value), Is.True);
            Assert.That(value, Is.EqualTo("2"));
        }

        [Test]
        public void TryGetValue_MissingId_ReturnsFalse()
        {
            var data = new SettingsData();
            Assert.That(data.TryGetValue("missing", out _), Is.False);
            Assert.That(data.TryGetValue(null, out _), Is.False);
        }

        [Test]
        public void Remove_ExistingId_RemovesIt()
        {
            var data = new SettingsData();
            data.Set("a", "1");

            Assert.That(data.Remove("a"), Is.True);
            Assert.That(data.Count, Is.Zero);
            Assert.That(data.Remove("a"), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Set_InvalidId_Throws(string id)
        {
            var data = new SettingsData();
            Assert.Throws<ArgumentException>(() => data.Set(id, "1"));
        }

        [Test]
        public void Set_NullValue_Throws()
        {
            var data = new SettingsData();
            Assert.Throws<ArgumentNullException>(() => data.Set("a", null));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var original = new SettingsData();
            original.Set("a", "1");

            SettingsData copy = original.Clone();
            copy.Set("a", "2");
            copy.Set("b", "3");

            Assert.That(original.TryGetValue("a", out string value), Is.True);
            Assert.That(value, Is.EqualTo("1"));
            Assert.That(original.Count, Is.EqualTo(1));
        }

        [Test]
        public void JsonUtility_RoundTrips()
        {
            var data = new SettingsData();
            data.Set("a", "1");
            data.Set("b", "1920x1080@144");

            SettingsData loaded = JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(data));

            Assert.That(loaded.Version, Is.EqualTo(SettingsData.CurrentVersion));
            Assert.That(loaded.Count, Is.EqualTo(2));
            Assert.That(loaded.TryGetValue("b", out string value), Is.True);
            Assert.That(value, Is.EqualTo("1920x1080@144"));
        }

        [Test]
        public void Normalize_RemovesInvalidEntriesAndKeepsLastDuplicate()
        {
            const string json =
                "{\"version\":0,\"entries\":[" +
                "{\"id\":\"a\",\"value\":\"1\"}," +
                "{\"id\":\"\",\"value\":\"x\"}," +
                "{\"id\":\"a\",\"value\":\"2\"}," +
                "{\"id\":\"b\",\"value\":\"3\"}]}";
            SettingsData data = JsonUtility.FromJson<SettingsData>(json);

            data.Normalize();

            Assert.That(data.Version, Is.EqualTo(SettingsData.CurrentVersion));
            Assert.That(data.Count, Is.EqualTo(2));
            Assert.That(data.TryGetValue("a", out string a), Is.True);
            Assert.That(a, Is.EqualTo("2"));
            Assert.That(data.TryGetValue("b", out string b), Is.True);
            Assert.That(b, Is.EqualTo("3"));
        }

        [Test]
        public void MissingEntriesInJson_BehavesAsEmpty()
        {
            SettingsData data = JsonUtility.FromJson<SettingsData>("{\"version\":1}");

            Assert.That(data.Count, Is.Zero);
            Assert.DoesNotThrow(() => data.Set("a", "1"));
        }
    }
}