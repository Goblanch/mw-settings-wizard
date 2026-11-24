#if MW_SETTINGS_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    /// <summary>
    /// Creates a URP Asset for a test and makes it the active pipeline. Dispose restores the previous one.
    /// The CI project has the URP package but no asset assigned, so tests bring their own.
    /// </summary>
    internal sealed class TestUrpPipeline : IDisposable
    {
        readonly RenderPipelineAsset previous;
        readonly UniversalRendererData rendererData;

        public TestUrpPipeline()
        {
            previous = QualitySettings.renderPipeline;
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            Asset = UniversalRenderPipelineAsset.Create(rendererData);
            Asset.name = "TestUrpAsset";
            QualitySettings.renderPipeline = Asset;
        }

        /// <summary>The "project" asset in the test: the one the handle copies.</summary>
        public UniversalRenderPipelineAsset Asset { get; }

        public void Dispose()
        {
            QualitySettings.renderPipeline = previous;
            Object.Destroy(Asset);
            Object.Destroy(rendererData);
        }
    }
}
#endif