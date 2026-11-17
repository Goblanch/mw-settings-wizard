using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class ToggleSettingTests
    {
        FakeToggleSetting setting;
        SettingsContext context;

        [SetUp]
        public void SetUp()
        {
            setting = ScriptableObject.CreateInstance<FakeToggleSetting>();
            context = new SettingsContext();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(setting);

        [Test]
        public void GetDefault_ReturnsSerializedDefault()
        {
            var so = new SerializedObject(setting);
            so.FindProperty("defaultValue").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(setting.GetDefault(context), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Serialize_RoundTrips(bool value)
        {
            Assert.That(setting.TryDeserialize(setting.Serialize(value), out bool result), Is.True);
            Assert.That(result, Is.EqualTo(value));
        }

        [TestCase("maybe")]
        [TestCase("")]
        [TestCase(null)]
        public void TryDeserialize_InvalidInput_ReturnsFalse(string raw)
        {
            Assert.That(setting.TryDeserialize(raw, out _), Is.False);
        }
    }
}