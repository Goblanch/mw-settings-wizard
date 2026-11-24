using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FrameRateLimitSettingTests
    {
        TestAssets assets;
        FrameRateLimitSetting setting;
        SettingsContext context;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            setting = assets.Setting<FrameRateLimitSetting>("display.fps");
            context = new SettingsContext();
        }

        [TearDown]
        public void TearDown() => assets.DestroyAll();

        void SetRates(params int[] rates) => assets.Configure(setting, so =>
        {
            var list = so.FindProperty("rates");
            list.arraySize = rates.Length;
            for (int i = 0; i < rates.Length; i++)
                list.GetArrayElementAtIndex(i).intValue = rates[i];
        });

        void SetDefaultKey(string key) =>
            assets.Configure(setting, so => so.FindProperty("defaultKey").stringValue = key);

        string[] Keys() => setting.GetOptions(context).Select(option => option.Key).ToArray();

        [Test]
        public void Options_AreSortedUniquePositiveRatesThenUnlimited()
        {
            SetRates(144, 60, -1, 0, 60, 30);

            Assert.That(Keys(), Is.EqualTo(new[] { "30", "60", "144", FrameRateLimitSetting.UnlimitedKey }));
        }

        [Test]
        public void RateOptions_HaveFpsLabels()
        {
            SetRates(60);

            Assert.That(setting.GetOptions(context)[0].RawLabel, Is.EqualTo("60 FPS"));
        }

        [Test]
        public void NoRates_OnlyUnlimited()
        {
            SetRates();

            Assert.That(Keys(), Is.EqualTo(new[] { FrameRateLimitSetting.UnlimitedKey }));
        }

        [Test]
        public void Default_WithoutDefaultKey_IsUnlimited()
        {
            Assert.That(setting.GetDefault(context), Is.EqualTo(FrameRateLimitSetting.UnlimitedKey));
        }

        [Test]
        public void Default_UsesInspectorKeyWhenItIsAnOption()
        {
            SetDefaultKey("60");

            Assert.That(setting.GetDefault(context), Is.EqualTo("60"));
        }

        [Test]
        public void Default_InspectorKeyNotAnOption_IsUnlimited()
        {
            SetDefaultKey("75");

            Assert.That(setting.GetDefault(context), Is.EqualTo(FrameRateLimitSetting.UnlimitedKey));
        }

        [TestCase("60", true, 60)]
        [TestCase("144", true, 144)]
        [TestCase(FrameRateLimitSetting.UnlimitedKey, false, 0)]
        [TestCase("0", false, 0)]
        [TestCase("-30", false, 0)]
        [TestCase(null, false, 0)]
        public void TryGetRate_ParsesOnlyPositiveRates(string key, bool expected, int expectedRate)
        {
            Assert.That(FrameRateLimitSetting.TryGetRate(key, out int rate), Is.EqualTo(expected));
            if (expected)
                Assert.That(rate, Is.EqualTo(expectedRate));
        }
    }
}