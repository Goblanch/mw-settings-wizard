using System.Collections.Generic;
using MiradorWorks.Settings.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiradorWorks.Settings.URP.Editor
{
    /// <summary>
    /// Risk R3: if a URP Asset has main light shadows disabled, builds strip the shadow shaders
    /// and Shadow Quality cannot turn shadows on.
    /// </summary>
    internal sealed class UrpShadowsCheck : SettingsProfileCheck
    {
        public override void Collect(SettingsProfile profile, List<string> warnings)
        {
            if (!UsesShadowQuality(profile))
                return;

            var checkedAssets = new HashSet<UniversalRenderPipelineAsset>();
            Check(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset, checkedAssets, warnings);

            for (int i = 0; i < QualitySettings.count; i++)
                Check(QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset, checkedAssets, warnings);
        }

        static bool UsesShadowQuality(SettingsProfile profile)
        {
            foreach (SettingDefinition setting in profile.Settings)
            {
                if (setting is ShadowQualitySetting)
                    return true;
            }

            return false;
        }

        static void Check(UniversalRenderPipelineAsset asset, HashSet<UniversalRenderPipelineAsset> checkedAssets, List<string> warnings)
        {
            if (asset == null || !checkedAssets.Add(asset))
                return;

            if (!asset.supportsMainLightShadows)
            {
                warnings.Add(
                    $"URP Asset '{asset.name}' has main light shadows disabled. Builds strip the shadow shaders, " +
                    "so Shadow Quality cannot turn shadows on. Enable Lighting > Main Light > Cast Shadows in that asset.");
            }
        }
    }
}