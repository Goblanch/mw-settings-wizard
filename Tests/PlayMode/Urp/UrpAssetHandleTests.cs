#if MW_SETTINGS_URP
using System;
using System.Collections;
using MiradorWorks.Settings.URP;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    public sealed class UrpAssetHandleTests
    {
        RenderPipelineAsset previousQualityPipeline;
        UniversalRendererData rendererData;
        UniversalRenderPipelineAsset testAsset;
        UrpAssetHandle handle;
        SettingsProfile profile;
        SettingsService service;

        [SetUp]
        public void SetUp()
        {
            previousQualityPipeline = QualitySettings.renderPipeline;

            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            testAsset = UniversalRenderPipelineAsset.Create(rendererData);
            testAsset.name = "TestUrpAsset";
            testAsset.renderScale = 1f;
            testAsset.shadowDistance = 42f;

            QualitySettings.renderPipeline = testAsset;
        }

        [TearDown]
        public void TearDown()
        {
            handle?.Dispose();
            handle = null;
            service?.Dispose();
            service = null;

            QualitySettings.renderPipeline = previousQualityPipeline;

            if (profile != null) Object.Destroy(profile);
            Object.Destroy(testAsset);
            Object.Destroy(rendererData);
        }

        [Test]
        public void CreateFromActive_AssignsACopyOfTheActiveAsset()
        {
            handle = UrpAssetHandle.CreateFromActive();

            Assert.That(handle.Original, Is.SameAs(testAsset));
            Assert.That(handle.Asset, Is.Not.SameAs(testAsset));
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(handle.Asset));
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(handle.Asset));
            Assert.That(UrpAssetHandle.IsUrpActive, Is.True);
        }

        [Test]
        public void Copy_StartsWithTheOriginalValues()
        {
            handle = UrpAssetHandle.CreateFromActive();

            Assert.That(handle.Asset.shadowDistance, Is.EqualTo(42f));
        }

        [Test]
        public void ChangingTheCopy_DoesNotTouchTheOriginal()
        {
            handle = UrpAssetHandle.CreateFromActive();

            handle.Asset.renderScale = 0.5f;

            Assert.That(testAsset.renderScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Dispose_RestoresThePipelineAndDestroysTheCopy()
        {
            handle = UrpAssetHandle.CreateFromActive();
            UniversalRenderPipelineAsset copy = handle.Asset;

            handle.Dispose();
            handle = null;

            Assert.That(QualitySettings.renderPipeline, Is.SameAs(testAsset));

            yield return null; // Object.Destroy takes effect at the end of the frame.

            Assert.That(copy == null, Is.True);
        }

        [Test]
        public void Dispose_Twice_IsSafe()
        {
            handle = UrpAssetHandle.CreateFromActive();

            handle.Dispose();

            Assert.DoesNotThrow(() => handle.Dispose());
        }

        [Test]
        public void Get_SharesOneHandlePerServiceAndDisposesItWithTheService()
        {
            profile = SettingsProfile.Create(Array.Empty<SettingDefinition>());
            service = new SettingsService(profile, new InMemorySettingsStorage());
            service.Initialize();

            UrpAssetHandle first = UrpAssetHandle.Get(service.Context);
            UrpAssetHandle second = UrpAssetHandle.Get(service.Context);
            Assert.That(second, Is.SameAs(first));
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(first.Asset));

            service.Dispose();
            service = null;

            Assert.That(QualitySettings.renderPipeline, Is.SameAs(testAsset));
        }

        [Test]
        public void CreateFromActive_WithoutUrp_Throws()
        {
            QualitySettings.renderPipeline = null;
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                Assert.Ignore("The project's default pipeline is URP, so there is always one active.");

            Assert.Throws<InvalidOperationException>(() => UrpAssetHandle.CreateFromActive());
        }
    }
}
#endif