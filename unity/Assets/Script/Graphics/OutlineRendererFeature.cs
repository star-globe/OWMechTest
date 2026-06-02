using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings.outlineMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Outline");
            Blit(cmd, ref renderingData, _settings.outlineMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
