using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class JsonFileSettingsStorageTests
    {
        string directory;
        string path;
        JsonFileSettingsStorage storage;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "MiradorWorksSettingsTests", Guid.NewGuid().ToString("N"));
            path = Path.Combine(directory, "settings.json");
            storage = new JsonFileSettingsStorage(path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        static SettingsData DataWith(string id, string value)
        {
            var data = new SettingsData();
            data.Set(id, value);
            return data;
        }

        [Test]
        public void TryLoad_MissingFile_ReturnsFalse()
        {
            Assert.That(storage.TryLoad(out SettingsData data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void Save_ThenTryLoad_RoundTrips()
        {
            storage.Save(DataWith("graphics.vsync", "true"));

            Assert.That(storage.TryLoad(out SettingsData loaded), Is.True);
            Assert.That(loaded.TryGetValue("graphics.vsync", out string value), Is.True);
            Assert.That(value, Is.EqualTo("true"));
        }

        [Test]
        public void Save_CreatesMissingDirectory()
        {
            Assert.That(Directory.Exists(directory), Is.False);

            storage.Save(DataWith("a", "1"));

            Assert.That(File.Exists(path), Is.True);
        }

        [Test]
        public void Save_LeavesNoTempFile()
        {
            storage.Save(DataWith("a", "1"));
            storage.Save(DataWith("a", "2"));

            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void Save_OverwritesPreviousFile()
        {
            storage.Save(DataWith("a", "1"));
            storage.Save(DataWith("a", "2"));

            storage.TryLoad(out SettingsData loaded);
            Assert.That(loaded.TryGetValue("a", out string value), Is.True);
            Assert.That(value, Is.EqualTo("2"));
        }

        [Test]
        public void TryLoad_CorruptFile_ReturnsFalseAndKeepsBackup()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "not json {{{");
            LogAssert.Expect(LogType.Warning, new Regex("corrupt"));

            Assert.That(storage.TryLoad(out SettingsData data), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo("not json {{{"));
        }

        [Test]
        public void TryLoad_NewerVersion_LoadsAndWarns()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "{\"version\":99,\"entries\":[{\"id\":\"a\",\"value\":\"1\"}]}");
            LogAssert.Expect(LogType.Warning, new Regex("newer"));

            Assert.That(storage.TryLoad(out SettingsData data), Is.True);
            Assert.That(data.TryGetValue("a", out string value), Is.True);
            Assert.That(value, Is.EqualTo("1"));
        }
    }
}