using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    /// <summary>Changes AudioListener.volume, so it lives in PlayMode and restores it afterwards (D27).</summary>
    public sealed class ListenerChannelTests
    {
        ListenerChannel channel;
        float originalVolume;

        [SetUp]
        public void SetUp()
        {
            originalVolume = AudioListener.volume;
            channel = ScriptableObject.CreateInstance<ListenerChannel>();
        }

        [TearDown]
        public void TearDown()
        {
            AudioListener.volume = originalVolume;
            Object.Destroy(channel);
        }

        [Test]
        public void Apply_SetsTheListenerVolume()
        {
            channel.Apply(0.25f);

            Assert.That(AudioListener.volume, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void Apply_ClampsToZeroOne()
        {
            channel.Apply(3f);

            Assert.That(AudioListener.volume, Is.EqualTo(1f));
        }

        [Test]
        public void Restore_GoesBackToFullVolume()
        {
            channel.Apply(0.25f);
            channel.Restore();

            Assert.That(AudioListener.volume, Is.EqualTo(1f));
        }
    }
}