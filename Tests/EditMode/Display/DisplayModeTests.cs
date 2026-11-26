using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class DisplayModesTests
    {
        [TestCase(DisplayModes.Borderless, FullScreenMode.FullScreenWindow)]
        [TestCase(DisplayModes.Exclusive, FullScreenMode.ExclusiveFullScreen)]
        [TestCase(DisplayModes.Windowed, FullScreenMode.Windowed)]
        [TestCase(DisplayModes.Maximized, FullScreenMode.MaximizedWindow)]
        public void TryGetMode_KnownKey_ReturnsMode(string key, FullScreenMode expected)
        {
            Assert.That(DisplayModes.TryGetMode(key, out FullScreenMode mode), Is.True);
            Assert.That(mode, Is.EqualTo(expected));
        }

        [TestCase("fullscreen")]
        [TestCase("")]
        [TestCase(null)]
        public void TryGetMode_UnknownKey_ReturnsFalse(string key)
        {
            Assert.That(DisplayModes.TryGetMode(key, out _), Is.False);
        }

        [Test]
        public void Windows_OffersExclusiveButNotMaximized()
        {
            Assert.That(DisplayModes.KeysFor(RuntimePlatform.WindowsPlayer),
                Is.EqualTo(new[] { DisplayModes.Borderless, DisplayModes.Exclusive, DisplayModes.Windowed }));
        }

        [Test]
        public void Mac_OffersMaximizedButNotExclusive()
        {
            Assert.That(DisplayModes.KeysFor(RuntimePlatform.OSXPlayer),
                Is.EqualTo(new[] { DisplayModes.Borderless, DisplayModes.Maximized, DisplayModes.Windowed }));
        }

        [Test]
        public void Linux_OffersBorderlessAndWindowed()
        {
            Assert.That(DisplayModes.KeysFor(RuntimePlatform.LinuxPlayer),
                Is.EqualTo(new[] { DisplayModes.Borderless, DisplayModes.Windowed }));
        }

        [Test]
        public void NonDesktopPlatform_OffersNothing()
        {
            Assert.That(DisplayModes.KeysFor(RuntimePlatform.Android), Is.Empty);
            Assert.That(DisplayModes.IsDesktop(RuntimePlatform.Android), Is.False);
            Assert.That(DisplayModes.IsDesktop(RuntimePlatform.WindowsEditor), Is.True);
        }
    }
}