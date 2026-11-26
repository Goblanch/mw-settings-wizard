using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MiradorWorks.Settings.URP
{
    /// <summary>
    /// Runtime copy of the active URP asset. URP settings change the copy, never the project's asset.
    /// The copy is assigned as the current quality level's pipeline; Dispose restores what was there.
    /// Use <see cref="Get"/> so every setting of a service shares one copy.
    /// </summary>
    public sealed class UrpAssetHandle : IDisposable
    {
        readonly RenderPipelineAsset previousQualityPipeline;
        bool disposed;

        UrpAssetHandle(UniversalRenderPipelineAsset original)
        {
            Original = original;
            previousQualityPipeline = QualitySettings.renderPipeline;

            Asset = Object.Instantiate(original);
            Asset.name = original.name + " (Runtime Copy)";
            Asset.hideFlags = HideFlags.DontSave;

            QualitySettings.renderPipeline = Asset;
        }

        /// <summary>The project's asset. Read it, never change it./// </summary>
        public UniversalRenderPipelineAsset Original { get; }

        /// <summary>The runtime copy that URP settings modify./// </summary>
        public UniversalRenderPipelineAsset Asset { get; }

        /// <summary>Whether the pipeline in use right now is URP.</summary>
        public static bool IsUrpActive => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;

        /// <summary>
        /// The handle shared by every setting of this settings service, created on first use
        /// and disposed with the service.
        /// </summary>
        public static UrpAssetHandle Get(SettingsContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.GetOrAddService(CreateFromActive);
        }

        /// <summary>
        /// Copies the active URP asset. Prefer <see cref="Get"/>, which shares one copy per service.
        /// </summary>
        /// <returns></returns>
        public static UrpAssetHandle CreateFromActive()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset active)
            {
                throw new InvalidOperationException(
                    "No URP Asset is active. Assign one in Project Settings > Graphics or Quality.");
            }

            return new UrpAssetHandle(active);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            // If something else replaced the pipeline meanwhile (e.g. a quality level change), leave it alone.
            if (QualitySettings.renderPipeline == Asset)
                QualitySettings.renderPipeline = previousQualityPipeline;

            if (Asset == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(Asset);
            else
                Object.DestroyImmediate(Asset);
        }
    }
}