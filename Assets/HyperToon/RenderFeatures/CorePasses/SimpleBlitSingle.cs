using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HyperToon
{
// single blit
    public class SimpleBlitSingle : ScriptableRendererFeature
    {
        [SerializeField] private BlitPassSettings settings;
        private BlitPass blitPass;

        public override void Create()
        {
            blitPass = new BlitPass(settings, name);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            blitPass.Setup();
            renderer.EnqueuePass(blitPass);
        }
    }
}
