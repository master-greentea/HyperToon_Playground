using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    public class ViewSpaceNormalsTextureSettings
    {
        public RenderTextureFormat colorFormat;
        public int depthBufferBits;
        public Color backgroundColor;
        public FilterMode filterMode;
    }

    // ScreenSpaceOutlines variables & methods
    [SerializeField] private RenderPassEvent renderPassEvent;
    [SerializeField] private Material normalsMaterial;
    [SerializeField] private Material outlinesMaterial;
    [SerializeField] private ViewSpaceNormalsTextureSettings viewSpaceNormalsTextureSettings;
    [SerializeField] private LayerMask outlinesLayerMask;
    
    private ViewSpaceNormalsTexturePass viewSpaceNormalsTexturePass;
    private ScreenSpaceOutlinePass screenSpaceOutlinePass;
    
    public override void Create()
    {
        viewSpaceNormalsTexturePass = new ViewSpaceNormalsTexturePass(renderPassEvent, normalsMaterial, outlinesLayerMask, viewSpaceNormalsTextureSettings);
        screenSpaceOutlinePass = new ScreenSpaceOutlinePass(renderPassEvent, outlinesMaterial);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(viewSpaceNormalsTexturePass);
        renderer.EnqueuePass(screenSpaceOutlinePass);
    }
}

/// <summary>
/// Normals Pass
/// </summary>
public class ViewSpaceNormalsTexturePass : ScriptableRenderPass
{
    private ScreenSpaceOutlines.ViewSpaceNormalsTextureSettings normalsTextureSettings;
    private readonly List<ShaderTagId> shaderTagIdList;
    private readonly RTHandle normals;
    private readonly Material normalsMaterial;
    private FilteringSettings filteringSettings;
    
    public ViewSpaceNormalsTexturePass(RenderPassEvent renderPassEvent, Material normalsMaterial, LayerMask outlinesLayerMask, ScreenSpaceOutlines.ViewSpaceNormalsTextureSettings settings)
    {
        shaderTagIdList = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("LightweightForward"),
            new ShaderTagId("SRPDefaultUnlit")
        };
        this.renderPassEvent = renderPassEvent;
        normals = RTHandles.Alloc("_SceneViewSpaceNormals", name: "_SceneViewSpaceNormals");
        normalsTextureSettings = settings;
        filteringSettings = new FilteringSettings(RenderQueueRange.opaque, outlinesLayerMask);
        this.normalsMaterial = normalsMaterial;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // descriptor setup
        RenderTextureDescriptor normalsTextureDescriptor = cameraTextureDescriptor;
        normalsTextureDescriptor.colorFormat = normalsTextureSettings.colorFormat;
        normalsTextureDescriptor.depthBufferBits = normalsTextureSettings.depthBufferBits;
        
        cmd.GetTemporaryRT(Shader.PropertyToID(normals.name), normalsTextureDescriptor, normalsTextureSettings.filterMode);
        ConfigureTarget(normals);
        ConfigureClear(ClearFlag.All, normalsTextureSettings.backgroundColor);
    }

    public override void Execute(ScriptableRenderContext ctx, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("SceneViewSpaceNormalsTextureCreation")))
        {
            ctx.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            DrawingSettings drawSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            drawSettings.overrideMaterial = normalsMaterial;
            ctx.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
        }
        
        ctx.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(Shader.PropertyToID(normals.name));
    }
}

/// <summary>
/// Outlines Pass
/// </summary>
public class ScreenSpaceOutlinePass : ScriptableRenderPass
{
    private readonly Material screenSpaceOutlineMaterial;
    private RenderTargetIdentifier cameraColorTarget;
    private RenderTargetIdentifier temporaryBuffer;
    private int temporaryBufferID = Shader.PropertyToID("_TemporaryBuffer");
        
    public ScreenSpaceOutlinePass(RenderPassEvent renderPassEvent, Material outlineShader)
    {
        this.renderPassEvent = renderPassEvent;
        screenSpaceOutlineMaterial = outlineShader;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
        temporaryBuffer = temporaryBufferID;
    }

    public override void Execute(ScriptableRenderContext ctx, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines")))
        {
            Blit(cmd, cameraColorTarget, temporaryBuffer);
            Blit(cmd, temporaryBuffer, cameraColorTarget, screenSpaceOutlineMaterial);
        }
        ctx.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(temporaryBufferID);
    }
}
