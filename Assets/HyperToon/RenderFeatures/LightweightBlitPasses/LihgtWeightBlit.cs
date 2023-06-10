using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HyperToon {
	public class LightWeightBlit : ScriptableRendererFeature {

		public class BlitPass : ScriptableRenderPass {

			private readonly Material blitMaterial;
			private FilterMode filterMode { get; set; }

			private RenderTargetIdentifier source { get; set; }
			private RenderTargetIdentifier destination { get; set; }

			RTHandle m_TemporaryColorTexture;
			RTHandle m_DestinationTexture;
			string m_ProfilerTag;

			public BlitPass(RenderPassEvent renderPassEvent, string tag, Material blitMaterial) {
				this.renderPassEvent = renderPassEvent;
				this.blitMaterial = blitMaterial;
				m_ProfilerTag = tag;
				m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture");
				m_DestinationTexture = RTHandles.Alloc("_DestinationTexture", name: "_DestinationTexture");
			}

			public void Setup() {
				ConfigureInput(ScriptableRenderPassInput.Normal);
			}

			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
				CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
				RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
				opaqueDesc.depthBufferBits = 0;

				// Set Source / Destination
				var renderer = renderingData.cameraData.renderer;
				if (renderingData.cameraData.camera.cameraType == CameraType.Reflection) return;

				// note : Seems this has to be done in here rather than in AddRenderPasses to work correctly in 2021.2+
				source = renderer.cameraColorTarget;
				destination = renderer.cameraColorTarget;
				
				cmd.GetTemporaryRT(Shader.PropertyToID(m_DestinationTexture.name), opaqueDesc, filterMode);
				
				if (source == destination) {
					cmd.GetTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name), opaqueDesc, filterMode);
					Blit(cmd, source, m_TemporaryColorTexture.nameID, blitMaterial);
					Blit(cmd, m_TemporaryColorTexture.nameID, destination);
				} else {
					Blit(cmd, source, destination, blitMaterial);
				}

				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}

			public override void FrameCleanup(CommandBuffer cmd) {
				cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_DestinationTexture.name));
				if (source == destination) {
					cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name));
				}
			}
		}

		[SerializeField] public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
		[SerializeField] public Material blitMaterial;
		
		private BlitPass blitPass;

		public override void Create() {
			blitPass = new BlitPass(renderPassEvent, name, blitMaterial);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
			if (blitMaterial == null) {
				Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
				return;
			}
			blitPass.Setup();
			renderer.EnqueuePass(blitPass);
		}
	}
}
