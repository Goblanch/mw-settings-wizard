#if MW_SETTINGS_URP
using System.Linq;
using MiradorWorks.Settings.URP;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    public sealed class UrpSettingsTests
    {
        PlayModeAssets assets;
        TestUrpPipeline pipeline;
        SettingsService service;

        [SetUp]
        public void SetUp()
        {
            assets = new PlayModeAssets();
            pipeline = new TestUrpPipeline();
            pipeline.Asset.renderScale = 1f;
            pipeline.Asset.msaaSampleCount = 1;
            pipeline.Asset.shadowDistance = 50f;
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            assets.DestroyAll();
            pipeline.Dispose();
        }

        UniversalRenderPipelineAsset Copy => (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

        void Initialize(params SettingDefinition[] settings)
        {
            service = new SettingsService(assets.Profile(settings), new InMemorySettingsStorage());
            service.Initialize();
        }

        // ---------- render scale ----------

        [Test]
        public void RenderScale_HasItsOwnRangeAndDefault()
        {
            var renderScale = assets.Setting<RenderScaleSetting>("graphics.renderscale");

            Assert.That(renderScale.Min, Is.EqualTo(0.5f));
            Assert.That(renderScale.Max, Is.EqualTo(2f));
            Assert.That(renderScale.Step, Is.EqualTo(0.05f));
            Assert.That(renderScale.GetDefault(new SettingsContext()), Is.EqualTo(1f));
        }

        [Test]
        public void RenderScale_ChangesTheCopyOnly()
        {
            var renderScale = assets.Setting<RenderScaleSetting>("graphics.renderscale");
            Initialize(renderScale);

            service.Set(renderScale, 0.75f);

            Assert.That(Copy, Is.Not.SameAs(pipeline.Asset));
            Assert.That(Copy.renderScale, Is.EqualTo(0.75f).Within(1e-4f));
            Assert.That(pipeline.Asset.renderScale, Is.EqualTo(1f));
        }

        // ---------- MSAA ----------

        [TestCase(MsaaSetting.Off, 1)]
        [TestCase(MsaaSetting.X2, 2)]
        [TestCase(MsaaSetting.X4, 4)]
        [TestCase(MsaaSetting.X8, 8)]
        [TestCase("unknown", 1)]
        public void Msaa_SampleCountFor_MapsKeys(string key, int expected)
        {
            Assert.That(MsaaSetting.SampleCountFor(key), Is.EqualTo(expected));
        }

        [Test]
        public void Msaa_SetsSampleCountOnTheCopy()
        {
            var msaa = assets.Setting<MsaaSetting>("graphics.msaa");
            Initialize(msaa);

            service.Set(msaa, MsaaSetting.X4);

            Assert.That(Copy.msaaSampleCount, Is.EqualTo(4));
            Assert.That(pipeline.Asset.msaaSampleCount, Is.EqualTo(1));
        }

        // ---------- shadows ----------

        [Test]
        public void Shadows_DefaultIsTheHighestLevel()
        {
            var shadows = assets.Setting<ShadowQualitySetting>("graphics.shadows");

            Assert.That(shadows.GetDefault(new SettingsContext()), Is.EqualTo("high"));
        }

        [Test]
        public void Shadows_LevelSetsEveryShadowProperty()
        {
            var shadows = assets.Setting<ShadowQualitySetting>("graphics.shadows");
            Initialize(shadows);

            service.Set(shadows, "low");

            Assert.That(Copy.shadowDistance, Is.EqualTo(25f));
            Assert.That(Copy.shadowCascadeCount, Is.EqualTo(1));
            Assert.That(Copy.mainLightShadowmapResolution, Is.EqualTo(1024));
            Assert.That(Copy.additionalLightsShadowmapResolution, Is.EqualTo(512));
            Assert.That(pipeline.Asset.shadowDistance, Is.EqualTo(50f));
        }

        [Test]
        public void Shadows_Off_SetsDistanceToZero()
        {
            var shadows = assets.Setting<ShadowQualitySetting>("graphics.shadows");
            Initialize(shadows);

            service.Set(shadows, "off");

            Assert.That(Copy.shadowDistance, Is.Zero);
        }

        [Test]
        public void Shadows_OptionsSkipEmptyAndRepeatedKeys()
        {
            var shadows = assets.Setting<ShadowQualitySetting>("graphics.shadows");
            assets.Configure(shadows,
                "{\"tiers\":[" +
                "{\"key\":\"a\",\"distance\":10,\"cascades\":1,\"mainLightResolution\":1024,\"additionalLightsResolution\":512}," +
                "{\"key\":\"\",\"distance\":20,\"cascades\":1,\"mainLightResolution\":1024,\"additionalLightsResolution\":512}," +
                "{\"key\":\"a\",\"distance\":30,\"cascades\":1,\"mainLightResolution\":1024,\"additionalLightsResolution\":512}," +
                "{\"key\":\"b\",\"distance\":40,\"cascades\":2,\"mainLightResolution\":2048,\"additionalLightsResolution\":1024}]}");

            string[] keys = shadows.GetOptions(new SettingsContext()).Select(option => option.Key).ToArray();

            Assert.That(keys, Is.EqualTo(new[] { "a", "b" }));
        }

        // ---------- lifetime ----------

        [Test]
        public void AllSettings_ShareOneCopy()
        {
            var renderScale = assets.Setting<RenderScaleSetting>("graphics.renderscale");
            var msaa = assets.Setting<MsaaSetting>("graphics.msaa");
            Initialize(renderScale, msaa);
            UniversalRenderPipelineAsset copyAfterInitialize = Copy;

            service.Set(renderScale, 0.75f);
            service.Set(msaa, MsaaSetting.X2);

            Assert.That(Copy, Is.SameAs(copyAfterInitialize));
        }

        [Test]
        public void DisposingTheService_RestoresTheProjectPipeline()
        {
            var renderScale = assets.Setting<RenderScaleSetting>("graphics.renderscale");
            Initialize(renderScale);
            service.Set(renderScale, 0.75f);

            service.Dispose();
            service = null;

            Assert.That(QualitySettings.renderPipeline, Is.SameAs(pipeline.Asset));
            Assert.That(pipeline.Asset.renderScale, Is.EqualTo(1f));
        }

        [Test]
        public void WithoutUrp_SettingsAreUnsupported()
        {
            QualitySettings.renderPipeline = null;
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                Assert.Ignore("The project's default pipeline is URP, so there is always one active.");

            var renderScale = assets.Setting<RenderScaleSetting>("graphics.renderscale");

            Assert.That(renderScale.IsSupported(new SettingsContext()), Is.False);
        }
    }
}
#endif