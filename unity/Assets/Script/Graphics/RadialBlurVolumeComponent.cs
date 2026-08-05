using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
[VolumeComponentMenu("Post-processing Custom/RadialBlur")]
[VolumeRequiresRendererFeatures(typeof(RadialBlurRendererFeature))]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
[DisplayInfo(name = "RadialBlur")]
public sealed class RadialBlurVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new (0f, 0f, 1f);      // ブラー強度
    public ClampedIntParameter sampleCount = new (8, 4, 24);        // タップ数
    public ClampedIntParameter downsample = new (2, 0, 3);          // 0=1/1, 1=1/2, 2=1/4, 3=1/8
    public ClampedFloatParameter centerRadius = new (0.2f, 0f, 1f); // 画面中心のブラー除外半径

    public bool IsActive()
    {
        return intensity.GetValue<float>() > 0.0f;
    }
}
