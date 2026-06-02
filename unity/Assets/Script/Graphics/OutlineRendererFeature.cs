using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// URPレンダラーアセットに追加するスクリーンスペースアウトライン。
/// URP Renderer の Add Renderer Feature からアタッチしてください。
/// </summary>
public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material outlineMaterial;
    }

    public Settings settings = new Settings();
    OutlineRenderPass _pass;

    public override void Create()
    {
        _pass = new OutlineRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }

    class OutlineRenderPass : ScriptableRenderPass
    {
        readonly Settings _settings;

        public OutlineRenderPass(Settings settings)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_settings.outlineMaterial == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "OutlineTemp";
            desc.clearBuffer = false;
            var temp = renderGraph.CreateTexture(desc);

            // アウトライン適用: activeColor → temp
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline", out var passData))
            {
                passData.source = source;
                passData.material = _settings.outlineMaterial;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(temp, 0);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0));
            }

            // 結果をコピーバック: temp → activeColor
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("OutlineCopyBack", out var passData))
            {
                passData.source = temp;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(source, 0);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false));
            }
        }
    }
}
