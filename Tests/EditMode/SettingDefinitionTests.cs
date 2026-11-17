using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SettingDefinitionTests
    {
        FakeToggleSetting toggle;
        FakeRangeSetting range;
        FakeChoiceSetting choice;

        [SetUp]
        public void SetUp()
        {
            toggle = ScriptableObject.CreateInstance<FakeToggleSetting>();
            range = ScriptableObject.CreateInstance<FakeRangeSetting>();
            choice = ScriptableObject.CreateInstance<FakeChoiceSetting>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(toggle);
            Object.DestroyImmediate(range);
            Object.DestroyImmediate(choice);
        }

        [Test]
        public void NewDefinition_HasInmediateApplyAndNoConfirmation()
        {
            Assert.That(toggle.ApplyMode, Is.EqualTo(ApplyMode.Immediate));
            Assert.That(toggle.RequiresConfirmation, Is.False);
        }

        [Test]
        public void NewDefinition_HasNonNullLabels()
        {
            Assert.That(toggle.Label, Is.Not.Null);
            Assert.That(toggle.Description, Is.Not.Null);
        }

        [Test]
        public void ValueType_MatchesGenericArgument()
        {
            Assert.That(toggle.ValueType, Is.EqualTo(typeof(bool)));
            Assert.That(range.ValueType, Is.EqualTo(typeof(float)));
            Assert.That(choice.ValueType, Is.EqualTo(typeof(string)));
        }
    }
}