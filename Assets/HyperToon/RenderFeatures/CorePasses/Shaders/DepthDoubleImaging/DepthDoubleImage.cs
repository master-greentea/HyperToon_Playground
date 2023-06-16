using UnityEngine;
using System;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class DepthDoubleImage : MonoBehaviour {

    [HideInInspector] public Shader dofShader;
    
    private Material dofMaterial;

    void OnRenderImage (RenderTexture source, RenderTexture destination) {
        if (dofMaterial == null) {
            dofMaterial = new Material(dofShader);
            dofMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        Graphics.Blit(source, destination, dofMaterial);
    }
}
