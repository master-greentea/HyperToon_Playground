using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HyperToon
{
    public class HyperRFEssential : ScriptableRendererFeature
    {
        // Common
        
        [Header("Transparency")]
        // Transparency passes
        [SerializeField] private TransparencyPassSettings transparencyPassSettings;
        private TransparencyGrabPass grabPass;
        private TransparencyRenderPass renderPass;
        
        [Header("Outline")]
        // Outlines passes
        [SerializeField] private OutlinePassSettings outlinePassSettings;
        private Material normalsMaterial;
        private Material outlinesMaterial;
        private ViewSpaceNormalsTexturePass viewSpaceNormalsTexturePass;
        private ScreenSpaceOutlinePass screenSpaceOutlinePass;
        
        // Complete render feature
        public override void Create()
        {
            // transparency passes
            grabPass = new TransparencyGrabPass(transparencyPassSettings);
            renderPass = new TransparencyRenderPass(transparencyPassSettings);
            // outlines passes
            // create materials
            normalsMaterial = CoreUtils.CreateEngineMaterial("HyperToon/Hidden/HyperToon_ViewSpaceNormalsShader");
            outlinesMaterial = CoreUtils.CreateEngineMaterial("HyperToon/Hidden/HyperToon_OutlineShader");
            // create passes
            viewSpaceNormalsTexturePass = new ViewSpaceNormalsTexturePass(outlinePassSettings.OutlinesRenderPassEvent, normalsMaterial, 
                outlinePassSettings.OutlinesLayerMask, outlinePassSettings.NormalsTextureSettings);
            screenSpaceOutlinePass = new ScreenSpaceOutlinePass(outlinePassSettings.OutlinesRenderPassEvent, outlinesMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // transparency passes added
            renderer.EnqueuePass(grabPass);
            renderer.EnqueuePass(renderPass);
            // outlines passes added
            screenSpaceOutlinePass.Setup(outlinePassSettings.OutlineMaterialSettings);
            renderer.EnqueuePass(viewSpaceNormalsTexturePass);
            renderer.EnqueuePass(screenSpaceOutlinePass);
        }
    }
}
