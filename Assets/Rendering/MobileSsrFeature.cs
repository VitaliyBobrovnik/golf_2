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

		[SerializeField]
		[Range(2, 8)]
		private int m_downsample = 8;

		[SerializeField]
		[Range(8, 32)]
		private int m_steps = 12;

		[SerializeField]
		[Min(1f)]
		private float m_maxDistance = 22f;

		[SerializeField]
		[Min(0.01f)]
		private float m_thickness = 0.4f;

		[SerializeField]
		[Range(0f, 1f)]
		private float m_intensity = 0.65f;

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
			private const GraphicsFormat HdrFormat = GraphicsFormat.R16G16B16A16_SFloat;
			private const GraphicsFormatUsage ReflectionFormatUsage = GraphicsFormatUsage.Render | GraphicsFormatUsage.Sample | GraphicsFormatUsage.Linear;
			private const float BlurOffset = 1.2f;
			private const int TracePass = 0;
			private const int BlurPass = 1;
			private const int CompositePass = 2;

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

			public ReflectionPass(MobileSsrFeature feature)
			{
				m_feature = feature;
				renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
				requiresIntermediateTexture = true;
				ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);
				m_format = SystemInfo.IsFormatSupported(HdrFormat, ReflectionFormatUsage) ? HdrFormat : GraphicsFormat.R8G8B8A8_UNorm;
			}

			public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
			{
				UniversalResourceData resources = frameData.Get<UniversalResourceData>();
				if (resources.isActiveTargetBackBuffer || !resources.cameraOpaqueTexture.IsValid() ||
					!resources.cameraDepthTexture.IsValid() || !resources.activeDepthTexture.IsValid())
				{
					return;
				}

				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
				UniversalLightData lightData = frameData.Get<UniversalLightData>();
				DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(
					s_shaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
				drawing.overrideMaterial = m_feature.m_material;
				FilteringSettings filtering = new FilteringSettings(RenderQueueRange.opaque, -1, m_feature.m_surfaces.value);

				TextureDesc descriptor = CreateReflectionDescriptor(cameraData.cameraTargetDescriptor);
				SetShaderParameters(descriptor.width, descriptor.height);
				TextureHandle reflection = graph.CreateTexture(descriptor);
				descriptor.name = "Mobile SSR Blurred Reflection";
				TextureHandle blurred = graph.CreateTexture(descriptor);

				drawing.overrideMaterialPassIndex = TracePass;
				using (IRasterRenderGraphBuilder builder = AddDrawPass(
					"Mobile SSR Trace",
					graph,
					new RendererListParams(renderingData.cullResults, drawing, filtering)))
				{
					builder.UseTexture(resources.cameraDepthTexture);
					builder.UseTexture(resources.cameraOpaqueTexture);
					builder.SetRenderAttachment(reflection, 0);
					builder.SetGlobalTextureAfterPass(reflection, s_traceId);
				}

				drawing.overrideMaterialPassIndex = BlurPass;
				using (IRasterRenderGraphBuilder builder = AddDrawPass(
					"Mobile SSR Blur",
					graph,
					new RendererListParams(renderingData.cullResults, drawing, filtering)))
				{
					builder.UseGlobalTexture(s_traceId);
					builder.SetRenderAttachment(blurred, 0);
					builder.SetGlobalTextureAfterPass(blurred, s_reflectionId);
				}

				drawing.overrideMaterialPassIndex = CompositePass;
				using (IRasterRenderGraphBuilder builder = AddDrawPass(
					"Mobile SSR Composite",
					graph,
					new RendererListParams(renderingData.cullResults, drawing, filtering)))
				{
					builder.UseGlobalTexture(s_reflectionId);
					builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
					builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
				}
			}

			private TextureDesc CreateReflectionDescriptor(RenderTextureDescriptor cameraTarget)
			{
				int divisor = Mathf.Max(2, m_feature.m_downsample);
				return new TextureDesc(
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
			}

			private void SetShaderParameters(int width, int height)
			{
				m_feature.m_material.SetVector(s_settingsId, new Vector4(
					m_feature.m_maxDistance,
					m_feature.m_thickness,
					m_feature.m_intensity,
					m_feature.m_steps));
				m_feature.m_material.SetVector(s_blurOffsetId, new Vector4(BlurOffset / width, BlurOffset / height, 0f, 0f));
			}

			private static IRasterRenderGraphBuilder AddDrawPass(string name, RenderGraph graph, RendererListParams rendererList)
			{
				RendererListHandle list = graph.CreateRendererList(rendererList);
				IRasterRenderGraphBuilder builder = graph.AddRasterRenderPass<PassData>(name, out PassData data);
				data.rendererList = list;
				builder.UseRendererList(list);
				builder.SetRenderFunc<PassData>(DrawRendererList);
				return builder;
			}

			private static void DrawRendererList(PassData data, RasterGraphContext context)
			{
				context.cmd.DrawRendererList(data.rendererList);
			}

			private sealed class PassData
			{
				public RendererListHandle rendererList;
			}
		}
	}
}
