using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class RadialBlurRendererFeature : ScriptableRendererFeature
{
    #region FEATURE_FIELDS

    [SerializeField] Material _material;
    [SerializeField] RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    [SerializeField] bool _useLegacyGaussian = false;

    private RadialBlurPass _pass;

    #endregion

    #region FEATURE_METHODS

    public override void Create()
    {
        if (_material == null)
            return;

        _pass = new RadialBlurPass(_material, _useLegacyGaussian) { renderPassEvent = _renderPassEvent };
    }

    // Override the AddRenderPasses method to inject passes into the renderer. Unity calls AddRenderPasses once per camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null)
            return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pass.Dispose();
    }

    #endregion

    // Create the custom render pass.
    private class RadialBlurPass : ScriptableRenderPass
    {
        #region PASS_FIELDS
        private Material _material;
        private bool _useLegacyGaussian;
        #endregion

        public RadialBlurPass(Material material, bool useLegacyGaussian)
        {
            _material = material;
            _useLegacyGaussian = useLegacyGaussian;
            requiresIntermediateTexture = true;   // バックバッファ直接描画を抑止
        }

        public void Dispose()
        {
        }

        #region PASS_SHARED_RENDERING_CODE

        // Get the texture descriptor needed to create the temporary color copy texture.
        // This method is used in both the render graph system path and the Compatibility Mode path.
        private static RenderTextureDescriptor GetCopyPassTextureDescriptor(RenderTextureDescriptor desc)
        {
            // Avoid an unnecessary multisample anti-aliasing (MSAA) resolve before the main render pass.
            desc.msaaSamples = 1;

            // Avoid copying the depth buffer, as the main pass render in this example doesn't use depth.
            desc.depthBufferBits = (int)DepthBits.None;

            return desc;
        }

        #endregion

        #region PASS_NON_RENDER_GRAPH_PATH

        // Free the resources the camera uses.
        // This method is used only in the Compatibility Mode path.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        #endregion

        #region PASS_RENDER_GRAPH_PATH

        // Declare the resource the copy render pass uses.
        // This method is used only in the render graph system path.
        private class CopyPassData
        {
            public TextureHandle inputTexture;
        }

        // Declare the resources the main render pass uses.
        // This method is used only in the render graph system path.
        private class PassData
        {
            public TextureHandle source;
            public Material material;
            public float intensity;
            public int sampleCount;
            public float centerRadius;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // 1.カメラ種別でフィルタ
            if (cameraData.cameraType != CameraType.Game)
                return;

            // 2.バックバッファ直書き時は中間テクスチャが使えないので抜ける
            if (resourceData.isActiveTargetBackBuffer)
                return;

            // 3.Volumeから現在地を取得。無効なら何も積まない
            var volume = VolumeManager.instance.stack.GetComponent<RadialBlurVolumeComponent>();
            if (volume == null || !volume.IsActive())
                return;

            var source = resourceData.activeColorTexture;

            // 4.縮小バッファを作る
            var desc = renderGraph.GetTextureDesc(source);
            desc.width >>= volume.downsample.value;
            desc.height >>= volume.downsample.value;
            desc.name = "RadialBlurTemp";
            desc.clearBuffer = false;
            desc.msaaSamples = MSAASamples.None;
            desc.depthBufferBits = 0;
            desc.filterMode = FilterMode.Bilinear;  // 拡大して戻すので必須
            var temp = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("RadialBlur", out var passData))
            {
                passData.source = source;
                passData.material = _material;
                passData.intensity = volume.intensity.value;
                passData.sampleCount = volume.sampleCount.value;
                passData.centerRadius = volume.centerRadius.value;

                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(temp, 0);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                {
                    data.material.SetFloat("_Intensity", data.intensity);
                    data.material.SetInt("_SampleCount", data.sampleCount);
                    data.material.SetFloat("_CenterRadius", data.centerRadius);
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }

        #endregion
    }
}
