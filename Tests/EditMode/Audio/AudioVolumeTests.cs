using NUnit.Framework;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class AudioVolumeTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0.5f, -6.0206f)]
        [TestCase(0.1f, -20f)]
        public void ToDecibels_ConvertsLinearVolume(float linear, float decibels)
        {
            Assert.That(AudioVolume.ToDecibels(linear), Is.EqualTo(decibels).Within(0.001f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(0.00001f)]
        [TestCase(float.NaN)]
        public void ToDecibels_VeryQuietOrInvalid_IsSilence(float linear)
        {
            Assert.That(AudioVolume.ToDecibels(linear), Is.EqualTo(AudioVolume.SilenceDecibels));
        }

        [Test]
        public void ToDecibels_AboveOne_IsCappedAtZero()
        {
            Assert.That(AudioVolume.ToDecibels(2f), Is.EqualTo(0f));
        }
    }
}