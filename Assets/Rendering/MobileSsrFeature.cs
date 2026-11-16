using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Golf.Rendering
{
    public sealed class MobileSsrFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader m_shader;
        [SerializeField] private RenderingLayerMask m_surfaces = 2u;
        [SerializeField] private UniversalRenderPipelineAsset m_highQualityPipeline;
        [SerializeField, Range(2, 8)] private int m_downsample = 8;
        [SerializeField, Range(8, 32)] private int m_steps = 12;
        [SerializeField, Min(1f)] private float m_maxDistance = 22f;
        [SerializeField, Min(0.01f)] private float m_thickness = 0.4f;
        [SerializeField, Range(0f, 1f)] private float m_intensity = 0.65f;

        private Material m_material;
        private ReflectionPass m_pass;

        public override void Create()
        {
            CoreUtils.Destroy(m_material);
            m_material = m_shader != null ? CoreUtils.CreateEngineMaterial(m_shader) : null;
            m_pass = m_material != null ? new ReflectionPass(this) : null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (m_pass == null || GraphicsSettings.currentRenderPipeline != m_highQualityPipeline ||
                !m_shader.isSupported || m_intensity <= 0f || m_surfaces.value == 0 ||
                camera.cameraType != CameraType.Game || camera.orthographic || renderingData.cameraData.renderType != CameraRenderType.Base)
            {
                return;
            }

            renderer.EnqueuePass(m_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_material);
            m_material = null;
            m_pass = null;
        }

        private sealed class ReflectionPass : ScriptableRenderPass
        {
            private static readonly int s_settingsId = Shader.PropertyToID("_GolfSsrSettings");
            private static readonly int s_blurOffsetId = Shader.PropertyToID("_GolfSsrBlurOffset");
            private static readonly int s_traceId = Shader.PropertyToID("_GolfSsrTrace");
            private static readonly int s_reflectionId = Shader.PropertyToID("_GolfSsrReflection");
            private static readonly List<ShaderTagId> s_shaderTags = new()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };

            private readonly MobileSsrFeature m_feature;
            private readonly GraphicsFormat m_format;

            private sealed class PassData
            {
                internal RendererListHandle list;
            }

            internal ReflectionPass(MobileSsrFeature feature)
            {
                m_feature = feature;
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);

                const GraphicsFormat hdr = GraphicsFormat.R16G16B16A16_SFloat;
                const GraphicsFormatUsage usage = GraphicsFormatUsage.Render | GraphicsFormatUsage.Sample | GraphicsFormatUsage.Linear;
                m_format = SystemInfo.IsFormatSupported(hdr, usage) ? hdr : GraphicsFormat.R8G8B8A8_UNorm;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer || !resources.cameraOpaqueTexture.IsValid() ||
                    !resources.cameraDepthTexture.IsValid() || !resources.activeDepthTexture.IsValid())
                {
                    return;
                }

                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();
                var drawing = RenderingUtils.CreateDrawingSettings(
                    s_shaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                drawing.overrideMaterial = m_feature.m_material;
                var filtering = new FilteringSettings(RenderQueueRange.opaque, -1, m_feature.m_surfaces.value);

                m_feature.m_material.SetVector(s_settingsId, new Vector4(
                    m_feature.m_maxDistance, m_feature.m_thickness, m_feature.m_intensity, m_feature.m_steps));

                var cameraTarget = cameraData.cameraTargetDescriptor;
                int divisor = Mathf.Max(2, m_feature.m_downsample);
                var desc = new TextureDesc(
                    Mathf.Max(1, cameraTarget.width / divisor),
                    Mathf.Max(1, cameraTarget.height / divisor))
                {
                    name = "Mobile SSR Reflection",
                    colorFormat = m_format,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    clearColor = Color.clear
                };
                var reflection = graph.CreateTexture(desc);
                const float blurOffset = 1.2f;
                m_feature.m_material.SetVector(s_blurOffsetId, new Vector4(blurOffset / desc.width, blurOffset / desc.height, 0f, 0f));

                drawing.overrideMaterialPassIndex = 0;
                var traceList = graph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                using (var builder = graph.AddRasterRenderPass<PassData>("Mobile SSR Trace", out var data))
                {
                    data.list = traceList;
                    builder.UseRendererList(traceList);
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.UseTexture(resources.cameraOpaqueTexture);
                    builder.SetRenderAttachment(reflection, 0);
                    builder.SetGlobalTextureAfterPass(reflection, s_traceId);
                    builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) => context.cmd.DrawRendererList(pass.list));
                }

                desc.name = "Mobile SSR Blurred Reflection";
                var blurred = graph.CreateTexture(desc);
                drawing.overrideMaterialPassIndex = 1;
                var blurList = graph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                using (var builder = graph.AddRasterRenderPass<PassData>("Mobile SSR Blur", out var data))
                {
                    data.list = blurList;
                    builder.UseRendererList(blurList);
                    builder.UseGlobalTexture(s_traceId);
                    builder.SetRenderAttachment(blurred, 0);
                    builder.SetGlobalTextureAfterPass(blurred, s_reflectionId);
                    builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) => context.cmd.DrawRendererList(pass.list));
                }

                drawing.overrideMaterialPassIndex = 2;
                var compositeList = graph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                using (var builder = graph.AddRasterRenderPass<PassData>("Mobile SSR Composite", out var data))
                {
                    data.list = compositeList;
                    builder.UseRendererList(compositeList);
                    builder.UseGlobalTexture(s_reflectionId);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                    builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) => context.cmd.DrawRendererList(pass.list));
                }
            }
        }
    }
}
