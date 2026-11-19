using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class SettingIdTests
    {
        [TestCase("graphics.shadows")]
        [TestCase("display-mode")]
        [TestCase("fps_limit")]
        [TestCase("A1")]
        public void IsValid_AllowedCharacters_ReturnsTrue(string id)
        {
            Assert.That(SettingId.IsValid(id), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" graphics")]
        [TestCase("has space")]
        [TestCase("a/b")]
        [TestCase("año")]
        public void IsValid_InvalidIds_ReturnsFalse(string id)
        {
            Assert.That(SettingId.IsValid(id), Is.False);
        }

        [Test]
        public void IsValid_TooLong_ReturnsFalse()
        {
            Assert.That(SettingId.IsValid(new string('a', SettingId.MaxLength)), Is.True);
            Assert.That(SettingId.IsValid(new string('a', SettingId.MaxLength + 1)), Is.False);
        }
    }
}