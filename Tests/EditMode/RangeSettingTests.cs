using System.Globalization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class RangeSettingTests
    {
        FakeRangeSetting setting;
        SettingsContext context;

        [SetUp]
        public void SetUp() => context = new SettingsContext();

        [TearDown]
        public void TearDown()
        {
            if (setting != null)
                Object.DestroyImmediate(setting);
        }

        void Create(float min, float max, float step, float defaultValue)
        {
            setting = ScriptableObject.CreateInstance<FakeRangeSetting>();
            var so = new SerializedObject(setting);
            so.FindProperty("min").floatValue = min;
            so.FindProperty("max").floatValue = max;
            so.FindProperty("step").floatValue = step;
            so.FindProperty("defaultValue").floatValue = defaultValue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Sanitize_BelowMin_ClampsToMin()
        {
            Create(0.5f, 2f, 0f, 1f);
            Assert.That(setting.Sanitize(0.1f, context), Is.EqualTo(0.5f));
        }

        [Test]
        public void Sanitize_AboveMax_ClampsToMax()
        {
            Create(0.5f, 2f, 0f, 1f);
            Assert.That(setting.Sanitize(5f, context), Is.EqualTo(2f));
        }

        [TestCase(1.1f, 1f)]
        [TestCase(1.2f, 1.25f)]
        [TestCase(1.9f, 2f)]
        public void Sanitize_WithStep_SnapsToNearestStep(float input, float expected)
        {
            Create(0.5f, 2f, 0.25f, 1f);
            Assert.That(setting.Sanitize(input, context), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void Sanitize_WithoutStep_KeepsValue()
        {
            Create(0.5f, 2f, 0f, 1f);
            Assert.That(setting.Sanitize(1.37f, context), Is.EqualTo(1.37f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Sanitize_NotFinite_ReturnsDefault(float input)
        {
            Create(0.5f, 2f, 0f, 1.5f);
            Assert.That(setting.Sanitize(input, context), Is.EqualTo(1.5f));
        }

        [Test]
        public void Serialize_UsesInvariantCulture()
        {
            Create(0f, 2f, 0f, 1f);
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("es-ES");

                string raw = setting.Serialize(1.25f);

                Assert.That(raw, Is.EqualTo("1.25"));
                Assert.That(setting.TryDeserialize(raw, out float value), Is.True);
                Assert.That(value, Is.EqualTo(1.25f));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCase("abc")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase(null)]
        public void TryDeserialize_InvalidInput_ReturnsFalse(string raw)
        {
            Create(0f, 2f, 0f, 1f);
            Assert.That(setting.TryDeserialize(raw, out _), Is.False);
        }

        [Test]
        public void AreEqual_ToleratesFloatError()
        {
            Create(0f, 2f, 0f, 1f);
            Assert.That(setting.AreEqual(0.1f + 0.2f, 0.3f), Is.True);
        }
    }
}