using NUnit.Framework;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    /// <summary>
    /// These tests change real engine settings, so they live in PlayMode and restore the originals afterwards.
    /// </summary>
    public sealed class QualitySettingsTests
    {
        PlayModeAssets assets;
        SettingsService service;
        int originalVSyncCount;
        int originalTargetFrameRate;
        int originalMipmapLimit;

        [SetUp]
        public void SetUp()
        {
            assets = new PlayModeAssets();
            originalVSyncCount = QualitySettings.vSyncCount;
            originalTargetFrameRate = Application.targetFrameRate;
            originalMipmapLimit = QualitySettings.globalTextureMipmapLimit;
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();

            QualitySettings.vSyncCount = originalVSyncCount;
            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.globalTextureMipmapLimit = originalMipmapLimit;
        }

        void Initialize(params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), new InMemorySettingsStorage());
            service.Initialize();
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void VSync_SetsVSyncCount(bool value, int expected)
        {
            var vsync = assets.Setting<VSyncSetting>("display.vsync");
            assets.Configure(vsync, $"{{\"defaultValue\":{(!value ? "true" : "false")}}}");
            Initialize(vsync);

            service.Set(vsync, value);

            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(expected));
        }

        [Test]
        public void FrameRateLimit_Rate_SetsTargetFrameRate()
        {
            var fps = assets.Setting<FrameRateLimitSetting>("display.fps");
            Initialize(fps);

            service.Set(fps, "60");

            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
        }

        [Test]
        public void FrameRateLimit_Unlimited_SetsMinusOne()
        {
            var fps = assets.Setting<FrameRateLimitSetting>("display.fps");
            assets.Configure(fps, "{\"defaultKey\":\"60\"}");
            Initialize(fps);

            service.Set(fps, FrameRateLimitSetting.UnlimitedKey);

            Assert.That(Application.targetFrameRate, Is.EqualTo(-1));
        }

        [TestCase(TextureQualitySetting.Half, 1)]
        [TestCase(TextureQualitySetting.Eighth, 3)]
        public void TextureQuality_SetsGlobalMipmapLimit(string key, int expected)
        {
            var textures = assets.Setting<TextureQualitySetting>("graphics.textures");
            Initialize(textures);

            service.Set(textures, key);

            Assert.That(QualitySettings.globalTextureMipmapLimit, Is.EqualTo(expected));
        }
    }
}