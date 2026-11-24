using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class ResolutionOptionsTests
    {
        static Resolution Res(int width, int height, uint numerator, uint denominator = 1) => new Resolution
        {
            width = width,
            height = height,
            refreshRateRatio = new RefreshRate { numerator = numerator, denominator = denominator }
        };

        static string[] Keys(IEnumerable<ResolutionOption> options) => options.Select(o => o.Key).ToArray();

        // ---------- Build ----------

        [Test]
        public void Build_RoundsRefreshRateInKeyAndLabel()
        {
            List<ResolutionOption> options = ResolutionOptions.Build(new[] { Res(1920, 1080, 143998, 1000) }, 720, true);

            Assert.That(options[0].Key, Is.EqualTo("1920x1080@144"));
            Assert.That(options[0].Label, Is.EqualTo("1920 × 1080 @ 144 Hz"));
        }

        [Test]
        public void Build_RemovesDuplicates()
        {
            List<ResolutionOption> options = ResolutionOptions.Build(
                new[] { Res(1920, 1080, 60), Res(1920, 1080, 60) }, 720, true);

            Assert.That(options, Has.Count.EqualTo(1));
        }

        [Test]
        public void Build_SortsLargestFirst()
        {
            List<ResolutionOption> options = ResolutionOptions.Build(new[]
            {
                Res(1280, 720, 60), Res(2560, 1440, 144), Res(1920, 1080, 60), Res(1920, 1080, 144)
            }, 720, true);

            Assert.That(Keys(options), Is.EqualTo(new[]
            {
                "2560x1440@144", "1920x1080@144", "1920x1080@60", "1280x720@60"
            }));
        }

        [Test]
        public void Build_HidesSizesBelowMinimumHeight()
        {
            List<ResolutionOption> options = ResolutionOptions.Build(
                new[] { Res(800, 600, 60), Res(1920, 1080, 60) }, 720, true);

            Assert.That(Keys(options), Is.EqualTo(new[] { "1920x1080@60" }));
        }

        [Test]
        public void Build_AllBelowMinimumHeight_KeepsThemAll()
        {
            List<ResolutionOption> options = ResolutionOptions.Build(
                new[] { Res(800, 600, 60), Res(640, 480, 60) }, 720, true);

            Assert.That(options, Has.Count.EqualTo(2));
        }

        [Test]
        public void Build_WithoutRefreshRate_MergesSizesAndKeepsHighestRate()
        {
            List<ResolutionOption> options = ResolutionOptions.Build(
                new[] { Res(1920, 1080, 60), Res(1920, 1080, 144) }, 720, false);

            Assert.That(Keys(options), Is.EqualTo(new[] { "1920x1080" }));
            Assert.That(options[0].Resolution.refreshRateRatio.value, Is.EqualTo(144).Within(0.01));
            Assert.That(options[0].Label, Is.EqualTo("1920 × 1080"));
        }

        // ---------- keys ----------

        [TestCase("1920x1080@144", 1920, 1080, 144)]
        [TestCase("1920x1080", 1920, 1080, 0)]
        public void TryParseKey_ValidKey_ReturnsParts(string key, int width, int height, int hz)
        {
            Assert.That(ResolutionOptions.TryParseKey(key, out int w, out int h, out int r), Is.True);
            Assert.That((w, h, r), Is.EqualTo((width, height, hz)));
        }

        [TestCase("abc")]
        [TestCase("1920x")]
        [TestCase("x1080")]
        [TestCase("1920x1080@")]
        [TestCase("0x0")]
        [TestCase(null)]
        public void TryParseKey_InvalidKey_ReturnsFalse(string key)
        {
            Assert.That(ResolutionOptions.TryParseKey(key, out _, out _, out _), Is.False);
        }

        // ---------- FindClosest ----------

        [Test]
        public void FindClosest_PicksNearestSize()
        {
            string[] keys = { "2560x1440@144", "1920x1080@144", "1280x720@60" };

            Assert.That(ResolutionOptions.FindClosest("1920x1200@144", keys), Is.EqualTo("1920x1080@144"));
        }

        [Test]
        public void FindClosest_SameSize_PicksNearestRefreshRate()
        {
            string[] keys = { "1920x1080@60", "1920x1080@144" };

            Assert.That(ResolutionOptions.FindClosest("1920x1080@120", keys), Is.EqualTo("1920x1080@144"));
        }

        [Test]
        public void FindClosest_UnparseableKey_ReturnsNull()
        {
            Assert.That(ResolutionOptions.FindClosest("not-a-resolution", new[] { "1920x1080@60" }), Is.Null);
        }
    }
}