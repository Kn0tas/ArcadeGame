using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EchoThief.Sonar
{
    /// <summary>
    /// URP Scriptable Renderer Feature that injects the sonar post-process pass.
    /// Updated for Unity 6 (6000.0+) using full RenderGraph API with RasterRenderPass.
    /// </summary>
    public class SonarRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Material using the SonarPostProcess shader.")]
            public Material sonarMaterial;

            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public Settings settings = new Settings();
        private SonarRenderPass _renderPass;

        public override void Create()
        {
            _renderPass = new SonarRenderPass(settings);
            _renderPass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.sonarMaterial == null)
            {
                Debug.LogWarning("[SonarRendererFeature] Sonar material is not assigned.");
                return;
            }
            renderer.EnqueuePass(_renderPass);
        }
    }

    public class SonarRenderPass : ScriptableRenderPass
    {
        private class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        private readonly SonarRendererFeature.Settings _settings;

        public SonarRenderPass(SonarRendererFeature.Settings settings)
        {
            _settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_settings.sonarMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
            // Cannot blit from/to the backbuffer directly
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle source = resourceData.activeColorTexture;

            // Create temporary texture for the effect output
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_SonarTempTexture";
            desc.clearBuffer = false;
            TextureHandle dest = renderGraph.CreateTexture(desc);

                // Pass 1: Apply Sonar Effect
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Sonar Pass", out var passData))
                {
                    passData.material = _settings.sonarMaterial;
                    passData.source = source;
                    
                    // Read from source, Write to destination
                    builder.UseTexture(source);
                    builder.SetRenderAttachment(dest, 0);
                    
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        // Blitter.BlitTexture executes a fullscreen triangle draw
                        // Source texture is bound to _BlitTexture by default in URP's Blit.hlsl
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                    });
                }

            // Pass 2: Copy Back to Camera Target
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Sonar Copy Back", out var passData))
            {
                // No material needed for simple copy, or use null to trigger default blit
                passData.material = null; 
                passData.source = dest;

                builder.UseTexture(dest);
                builder.SetRenderAttachment(source, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
        }
    }
}
