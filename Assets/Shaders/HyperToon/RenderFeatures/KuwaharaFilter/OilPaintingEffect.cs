using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class OilPaintingEffect : ScriptableRendererFeature
{
    private static readonly LayerMask AllLayers = ~0;
    private const int FilterKernelSize = 32;
    public Settings settings;
    private DepthOnlyPass depthOnlyPass;
    private OilPaintingEffectPass renderPass;
    private RTHandle depthTexture;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (Application.isEditor && (renderingData.cameraData.camera.name == "SceneCamera" ||
                                     renderingData.cameraData.camera.name == "Preview Scene Camera")) return;
        
        depthOnlyPass.Setup(renderingData.cameraData.cameraTargetDescriptor, depthTexture);
        renderer.EnqueuePass(depthOnlyPass);
        
        renderPass.Setup(settings);
        renderer.EnqueuePass(renderPass);
    }

    public override void Create()
    {
        var structureTensorMaterial = CoreUtils.CreateEngineMaterial("Hidden/Oil Painting/Structure Tensor");
        var kuwaharaFilterMaterial = CoreUtils.CreateEngineMaterial("Hidden/Oil Painting/Anisotropic Kuwahara Filter");
        var lineIntegralConvolutionMaterial = CoreUtils.CreateEngineMaterial("Hidden/Oil Painting/Line Integral Convolution");
        var compositorMaterial = CoreUtils.CreateEngineMaterial("Hidden/Oil Painting/Compositor");

        renderPass = new OilPaintingEffectPass(structureTensorMaterial, kuwaharaFilterMaterial, 
            lineIntegralConvolutionMaterial, compositorMaterial);
        
        renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        var texture = new Texture2D(FilterKernelSize, FilterKernelSize, TextureFormat.RFloat, true);
        InitializeFilterKernelTexture(texture, FilterKernelSize,
            settings.anisotropicKuwaharaFilterSettings.filterKernelSectors,
            settings.anisotropicKuwaharaFilterSettings.filterKernelSmoothness);
        
        settings.anisotropicKuwaharaFilterSettings.filterKernelTexture = texture;
        
        depthOnlyPass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPostProcessing, RenderQueueRange.all, AllLayers);
        
        depthTexture = RTHandles.Alloc("_CameraDepthTexture", name: "_CameraDepthTexture");
    } 
    private static void InitializeFilterKernelTexture(Texture2D texture, int kernelSize, int sectorCount, float smoothing) 
    { 
        for (int j = 0; j < texture.height; j++) {
            
            for (int i = 0; i < texture.width; i++)
            { 
                float x = i - 0.5f * texture.width + 0.5f; 
                float y = j - 0.5f * texture.height + 0.5f; 
                float r = Mathf.Sqrt(x * x + y * y);

                float a = 0.5f * Mathf.Atan2(y, x) / Mathf.PI;

                if (a > 0.5f)
                {
                    a -= 1f; 
                }

                if (a < -0.5f)
                {
                    a += 1f;
                }

                if ((Mathf.Abs(a) <= 0.5f / sectorCount) && (r < 0.5f * kernelSize))
                {
                    texture.SetPixel(i, j, Color.red);
                }
                else
                {
                    texture.SetPixel(i, j, Color.black);
                }
            }
        }
        
        float sigma = 0.25f * (kernelSize - 1);

        GaussianBlur(texture, sigma * smoothing);

        float maxValue = 0f;
        for (int j = 0; j < texture.height; j++)
        {
            for (int i = 0; i < texture.width; i++)
            {
                var x = i - 0.5f * texture.width + 0.5f;
                var y = j - 0.5f * texture.height + 0.5f;
                var r = Mathf.Sqrt(x * x + y * y);

                var color = texture.GetPixel(i, j);
              color *= Mathf.Exp(-0.5f * r * r / sigma / sigma);
                texture.SetPixel(i, j, color);

               if (color.r > maxValue)
                {
                    maxValue = color.r;
                }
            }
        }
        
        for (int j = 0; j < texture.height; j++) 
        { 
            for (int i = 0; i < texture.width; i++) 
            { 
                var color = texture.GetPixel(i, j); 
                color /= maxValue; 
                texture.SetPixel(i, j, color); 
            } 
        }
        
        texture.Apply(true, true); 
    }

    private static void GaussianBlur(Texture2D texture, float sigma)
    {
        float twiceSigmaSq = 2.0f * sigma * sigma;
        int halfWidth = Mathf.CeilToInt(2 * sigma);
        
        var colors = new Color[texture.width * texture.height];
        
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                int index = y * texture.width + x;

                float norm = 0;
                for (int i = -halfWidth; i <= halfWidth; i++)
                {
                    int xi = x + i;
                    if (xi < 0 || xi >= texture.width) continue;
                     for (int j = -halfWidth; j <= halfWidth; j++)
                     { 
                         int yj = y + j; 
                         if (yj < 0 || yj >= texture.height) continue;
                         
                         float distance = Mathf.Sqrt(i * i + j * j); 
                         float k = Mathf.Exp(-distance * distance / twiceSigmaSq);
                         
                         colors[index] += texture.GetPixel(xi, yj) * k; 
                         norm += k; 
                     }
                }
                
                colors[index] /= norm; 
            }
        }

        texture.SetPixels(colors);
    }

    [Serializable]
    public class Settings
    {
        public AnisotropicKuwaharaFilterSettings anisotropicKuwaharaFilterSettings;
        public EdgeFlowSettings edgeFlowSettings;
        public CompositorSettings compositorSettings;
    }

    [Serializable]
    public class AnisotropicKuwaharaFilterSettings
    {
        [Range(3, 8)] public int filterKernelSectors = 8;
        [Range(0f, 1f)] public float filterKernelSmoothness = 0.33f;
        [NonSerialized] public Texture2D filterKernelTexture;
        [Space(10)]
        [Range(2f, 12f)] public float filterRadius = 4f;
        [Range(2f, 16f)] public float filterSharpness = 8f;
        [Range(0.125f, 8f)] public float eccentricity = 1f;
        [Space(10)]
        [Range(1, 4)] public int iterations = 1;
    }

    [Serializable]
    public class EdgeFlowSettings
    {
        public Texture2D noiseTexture;
        [Space(10)]
        [Range(1, 64)] public int streamLineLength = 10;
        [Range(0f, 2f)] public float streamKernelStrength = .5f;
    }
    
    [Serializable]
    public class CompositorSettings
    {
        [Range(0f, 4f)] public float edgeContribution = 1f;
        [Range(0f, 4f)] public float flowContribution = 1f;
        [Range(0f, 4f)] public float depthContribution = 1f;
        [Space(10)]
        [Range(0.25f, 1f)] public float bumpPower = 0.8f;
        [Range(0f, 1f)] public float bumpIntensity = 0.4f;
    }
}

/// <summary>
/// Oil painting render pass
/// </summary>
public class OilPaintingEffectPass : ScriptableRenderPass
{
    private RenderTargetIdentifier source;
    private RenderTargetIdentifier destination;
    
    private RenderTexture structureTensorTex;
    private RenderTexture kuwaharaFilterTex;
    private RenderTexture edgeFlowTex;

    private readonly Material structureTensorMaterial;
    private readonly Material kuwaharaFilterMaterial;
    private readonly Material lineIntegralConvolutionMaterial;
    private readonly Material compositorMaterial;
    
    private int kuwaharaFilterIterations = 1;
    
    private FilteringSettings filteringSettings;
    
    public OilPaintingEffectPass(Material structureTensorMaterial, Material kuwaharaFilterMaterial, 
        Material lineIntegralConvolutionMaterial, Material compositorMaterial)
    {
        this.structureTensorMaterial = structureTensorMaterial;
        this.kuwaharaFilterMaterial = kuwaharaFilterMaterial;
        this.lineIntegralConvolutionMaterial = lineIntegralConvolutionMaterial;
        this.compositorMaterial = compositorMaterial;
    }
    
    public void Setup(OilPaintingEffect.Settings settings)
    {
        SetupKuwaharaFilter(settings.anisotropicKuwaharaFilterSettings);
        SetupLineIntegralConvolution(settings.edgeFlowSettings);
        SetupCompositor(settings.compositorSettings);
    }

    private void SetupKuwaharaFilter(OilPaintingEffect.AnisotropicKuwaharaFilterSettings kuwaharaFilterSettings)
    {
        kuwaharaFilterMaterial.SetInt("_FilterKernelSectors", kuwaharaFilterSettings.filterKernelSectors);
        kuwaharaFilterMaterial.SetTexture("_FilterKernelTex", kuwaharaFilterSettings.filterKernelTexture);
        kuwaharaFilterMaterial.SetFloat("_FilterRadius", kuwaharaFilterSettings.filterRadius);
        kuwaharaFilterMaterial.SetFloat("_FilterSharpness", kuwaharaFilterSettings.filterSharpness);
        kuwaharaFilterMaterial.SetFloat("_Eccentricity", kuwaharaFilterSettings.eccentricity);
        kuwaharaFilterIterations = kuwaharaFilterSettings.iterations;
    }
    
    private void SetupLineIntegralConvolution(OilPaintingEffect.EdgeFlowSettings edgeFlowSettings)
    {
        lineIntegralConvolutionMaterial.SetTexture("_NoiseTex", edgeFlowSettings.noiseTexture);
        lineIntegralConvolutionMaterial.SetFloat("_StreamLineLength", edgeFlowSettings.streamLineLength);
        lineIntegralConvolutionMaterial.SetFloat("_StreamKernelStrength", edgeFlowSettings.streamKernelStrength);
    }

    private void SetupCompositor(OilPaintingEffect.CompositorSettings compositorSettings)
    {
        compositorMaterial.SetFloat("_EdgeContribution", compositorSettings.edgeContribution);
        compositorMaterial.SetFloat("_FlowContribution", compositorSettings.flowContribution);
        compositorMaterial.SetFloat("_DepthContribution", compositorSettings.depthContribution);
        compositorMaterial.SetFloat("_BumpPower", compositorSettings.bumpPower);
        compositorMaterial.SetFloat("_BumpIntensity", compositorSettings.bumpIntensity);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        blitTargetDescriptor.depthBufferBits = 0;

        var renderer = renderingData.cameraData.renderer;

        source = renderer.cameraColorTarget;
        destination = renderer.cameraColorTarget;
        
        structureTensorTex = RenderTexture.GetTemporary(blitTargetDescriptor.width, blitTargetDescriptor.height, 0, RenderTextureFormat.ARGBFloat);
        kuwaharaFilterTex = RenderTexture.GetTemporary(blitTargetDescriptor);
        edgeFlowTex = RenderTexture.GetTemporary(blitTargetDescriptor.width, blitTargetDescriptor.height, 0, RenderTextureFormat.RFloat);
    }

    public override void Execute(ScriptableRenderContext ctx, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Oil Painting Effect");
        
        Blit(cmd, source, structureTensorTex, structureTensorMaterial, -1);
        
        kuwaharaFilterMaterial.SetTexture("_StructureTensorTex", structureTensorTex);
        Blit(cmd, source, kuwaharaFilterTex, kuwaharaFilterMaterial, -1);
        for (int i = 0; i < kuwaharaFilterIterations - 1; i++)
        { 
             Blit(cmd, kuwaharaFilterTex, kuwaharaFilterTex, kuwaharaFilterMaterial, -1);
        }
        
        Blit(cmd, structureTensorTex, edgeFlowTex, lineIntegralConvolutionMaterial, -1);
        
        compositorMaterial.SetTexture("_EdgeFlowTex", edgeFlowTex);
        
        Blit(cmd, kuwaharaFilterTex, destination, compositorMaterial, -1);

        ctx.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    public override void FrameCleanup(CommandBuffer cmd)
    {
        RenderTexture.ReleaseTemporary(structureTensorTex);
        RenderTexture.ReleaseTemporary(kuwaharaFilterTex);
        RenderTexture.ReleaseTemporary(edgeFlowTex);
    }
}
