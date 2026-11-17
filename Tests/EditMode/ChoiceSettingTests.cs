using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class ChoiceSettingTests
    {
        FakeChoiceSetting setting;
        SettingsContext context;

        [SetUp]
        public void SetUp()
        {
            setting = ScriptableObject.CreateInstance<FakeChoiceSetting>();
            setting.Options.Add(ChoiceOption.Text("a", "A"));
            setting.Options.Add(ChoiceOption.Text("b", "B"));
            setting.Options.Add(ChoiceOption.Text("c", "C"));
            context = new SettingsContext();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(setting);

        void SetDefaultKey(string key)
        {
            var so = new SerializedObject(setting);
            so.FindProperty("defaultKey").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void GetDefault_ExistingDefaultKey_ReturnsIt()
        {
            SetDefaultKey("b");
            Assert.That(setting.GetDefault(context), Is.EqualTo("b"));
        }

        [Test]
        public void GetDefault_MissingDefaultKey_ReturnsFirstOption()
        {
            SetDefaultKey("zzz");
            Assert.That(setting.GetDefault(context), Is.EqualTo("a"));
        }

        [Test]
        public void Sanitize_ExistingKey_KeepsIt()
        {
            Assert.That(setting.Sanitize("c", context), Is.EqualTo("c"));
        }

        [Test]
        public void Sanitize_MissingKey_UsesClosest()
        {
            setting.Closest = "c";
            Assert.That(setting.Sanitize("x", context), Is.EqualTo("c"));
        }

        [Test]
        public void Sanitize_MissingKeyAndInvalidClosest_ReturnsDefault()
        {
            SetDefaultKey("b");
            setting.Closest = "not-an-option";
            Assert.That(setting.Sanitize("x", context), Is.EqualTo("b"));
        }

        [Test]
        public void Sanitize_Null_ReturnsDefault()
        {
            SetDefaultKey("b");
            Assert.That(setting.Sanitize(null, context), Is.EqualTo("b"));
        }

        [Test]
        public void NoOptions_IsNotSupportedAndDefaultIsNull()
        {
            setting.Options.Clear();
            Assert.That(setting.IsSupported(context), Is.False);
            Assert.That(setting.GetDefault(context), Is.Null);
        }

        [Test]
        public void IndexOf_ReturnsOptionIndexOrMinusOne()
        {
            Assert.That(ChoiceSetting.IndexOf(setting.Options, "b"), Is.EqualTo(1));
            Assert.That(ChoiceSetting.IndexOf(setting.Options, "zzz"), Is.EqualTo(-1));
            Assert.That(ChoiceSetting.IndexOf(setting.Options, null), Is.EqualTo(-1));
        }

        [Test]
        public void Serialize_RoundTrips()
        {
            Assert.That(setting.TryDeserialize(setting.Serialize("b"), out string value), Is.True);
            Assert.That(value, Is.EqualTo("b"));
        }

        [Test]
        public void TryDeserialize_Empty_ReturnsFalse()
        {
            Assert.That(setting.TryDeserialize(string.Empty, out _), Is.False);
        }

        [Test]
        public void ChoiceOption_EmptyKey_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => ChoiceOption.Text("", "Empty"));
        }
    }
}