using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HyperToon;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

public class SkyboxSettings : ScriptableObject
{
    private const int Resolution = 128;

    [Header("Gradient")]
    [SerializeField] private Gradient nightDayGradient;
    [SerializeField] private Gradient horizonZenithGradient;
    [SerializeField] private Gradient sunHaloGradient;

    [Header("Sun")]
    [Range(0f, 1f)] [SerializeField] private float sunRadius = .05f;
    [Range(1f, 3f)] [SerializeField] private float sunIntensity = 1;
    [Header("Moon")]
    [Range(0f, 1f)] [SerializeField] private float moonRadius = .05f;
    [Range(0.01f, 1f)] [SerializeField] private float moonEdgeStrength = .05f;
    [Range(-16, 0)] [SerializeField] private float moonExposure = 0;
    [Range(0, 1)] [SerializeField] private float moonDarkside = .01f;
    [SerializeField] private Cubemap moonTexture;
    [Header("Clouds")]
    [SerializeField] private Cubemap cloudCubeMap;
    [Range(0f, .1f)] [SerializeField] private float cloudSpeed = .001f;
    [SerializeField] private bool cloudTurnOn = true;
    [Header("Stars")]
    [SerializeField] private Cubemap starCubeMap;
    [Range(0f, .1f)] [SerializeField] private float starSpeed = .001f;
    [Range(-16, 16)] [SerializeField] private int starExposure = 0;
    [Range(1f, 5f)] [SerializeField] private float starPower = 0;
    [Range(-90, 90)] [SerializeField] private int starLatitude = 0;
    
    private void OnValidate()
    {
        if (RenderSettings.skybox.shader.name != "HyperToon/Skybox/HyperToon_Skybox")
        {
            Debug.Log("Not using correct shaders!");
            return;
        }
        SetSkyboxValues();
    }

    private void SetSkyboxValues()
    {
        RenderSettings.skybox.SetFloat("_SunRadius", sunRadius);
        RenderSettings.skybox.SetFloat("_SunIntensity", sunIntensity);
        RenderSettings.skybox.SetFloat("_MoonRadius", moonRadius);
        RenderSettings.skybox.SetFloat("_MoonEdgeStrength", moonEdgeStrength);
        RenderSettings.skybox.SetFloat("_MoonExposure", moonExposure);
        RenderSettings.skybox.SetFloat("_MoonDarkside", moonDarkside);
        RenderSettings.skybox.SetTexture("_MoonTexture", moonTexture);
        RenderSettings.skybox.SetTexture("_CloudCubeMap", cloudCubeMap);
        RenderSettings.skybox.SetFloat("_CloudSpeed", cloudSpeed);
        RenderSettings.skybox.SetFloat("_CloudOn", cloudTurnOn ? 1 : 0);
        RenderSettings.skybox.SetTexture("_StarCubeMap", starCubeMap);
        RenderSettings.skybox.SetFloat("_StarSpeed", starSpeed);
        RenderSettings.skybox.SetFloat("_StarExposure", starExposure);
        RenderSettings.skybox.SetFloat("_StarPower", starPower);
        RenderSettings.skybox.SetFloat("_StarLatitude", starLatitude);
    }

    /// <summary>
    /// Parse hex string to color
    /// </summary>
    /// <param name="hex">hex string</param>
    /// <returns>color</returns>
    private Color GetColorFromHex(string hex)
    {
        Color col = Color.black;
        if (ColorUtility.TryParseHtmlString("#" + hex + "FF", out col))
        {
            return col;
        }
        return col;
    }
    
    /// <summary>
    /// Save single gradient to texture
    /// </summary>
    /// <param name="gradient">gradient</param>
    /// <param name="textureName">name of saved texture</param>
    private void SaveGradient(Gradient gradient, string textureName)
    {
        Texture2D tex = new Texture2D(Resolution, 1);
        for (int x = 0; x < Resolution; x++)
        {
            tex.SetPixel (x, 0, gradient.Evaluate (x / (float)Resolution));
        }
        tex.Apply();
 
        byte[] data = tex.EncodeToPNG();
        MonoScript ms = MonoScript.FromScriptableObject(this);
        string path = string.Format("{0}/{1}.png",  AssetDatabase.GetAssetPath(ms).Replace("Editor/SkyboxSettings.cs", "Skybox/Textures/SkyGradients"), textureName);
        File.WriteAllBytes(path, data);

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Reset and updates all gradients
    /// </summary>
    [ContextMenu("Reset Gradients")]
    public void ResetGradients()
    {
        // default alpha Keys
        GradientAlphaKey[] defaulAlphas = new GradientAlphaKey[2];
        defaulAlphas[0].alpha = 1f;
        defaulAlphas[1].alpha = 1f;
        defaulAlphas[0].time = 0f;
        defaulAlphas[1].time = 1f;
        
        GradientColorKey[] defaultSunZenithColors = new GradientColorKey[4];
        defaultSunZenithColors[0].color = GetColorFromHex("0D0E17");
        defaultSunZenithColors[1].color = GetColorFromHex("121823");
        defaultSunZenithColors[2].color = GetColorFromHex("61B0D8");
        defaultSunZenithColors[3].color = GetColorFromHex("4C90D8");
        defaultSunZenithColors[0].time = 0f;
        defaultSunZenithColors[1].time = .347f;
        defaultSunZenithColors[2].time = .721f;
        defaultSunZenithColors[3].time = 1f;
        nightDayGradient.SetKeys(defaultSunZenithColors, defaulAlphas);
        
        GradientColorKey[] horizonZenithColors = new GradientColorKey[6];
        horizonZenithColors[0].color = GetColorFromHex("15151A");
        horizonZenithColors[1].color = GetColorFromHex("284167");
        horizonZenithColors[2].color = GetColorFromHex("FE6E00");
        horizonZenithColors[3].color = GetColorFromHex("55BF49");
        horizonZenithColors[4].color = GetColorFromHex("A8D5EC");
        horizonZenithColors[5].color = GetColorFromHex("A7C7EA");
        horizonZenithColors[0].time = 0f;
        horizonZenithColors[1].time = .318f;
        horizonZenithColors[2].time = .485f;
        horizonZenithColors[3].time = .582f;
        horizonZenithColors[4].time = .635f;
        horizonZenithColors[5].time = 1f;
        horizonZenithGradient.SetKeys(horizonZenithColors, defaulAlphas);

        GradientColorKey[] defaultSunHaloColors = new GradientColorKey[5];
        defaultSunHaloColors[0].color = GetColorFromHex("000000");
        defaultSunHaloColors[1].color = GetColorFromHex("000000");
        defaultSunHaloColors[2].color = GetColorFromHex("FDCA13");
        defaultSunHaloColors[3].color = GetColorFromHex("42845B");
        defaultSunHaloColors[4].color = GetColorFromHex("65AA9E");
        defaultSunHaloColors[0].time = 0f;
        defaultSunHaloColors[1].time = .174f;
        defaultSunHaloColors[2].time = .506f;
        defaultSunHaloColors[3].time = .629f;
        defaultSunHaloColors[4].time = 1f;
        sunHaloGradient.SetKeys(defaultSunHaloColors, defaulAlphas);
        
        UpdateGradients();
    }

    /// <summary>
    /// Save all gradients to texture
    /// </summary>
    [ContextMenu("Update Gradients")]
    public void UpdateGradients()
    {
        SaveGradient(nightDayGradient, "NightDayGradient");
        SaveGradient(horizonZenithGradient, "HorizonZenithGradient");
        SaveGradient(sunHaloGradient, "SunHaloGradient");
    }
}

[CustomEditor(typeof(SkyboxSettings))]
public class SkyboxEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        HyperToonEditor.DrawHyperToonLogo();
        DrawDefaultInspector();
        GUILayout.Space(15);
        SkyboxSettings s = (SkyboxSettings)target;
        if (GUILayout.Button("Reset Gradients"))
        {
            s.ResetGradients();
        }
        if (GUILayout.Button("Update Gradients"))
        {
            s.UpdateGradients();
        }
    }
}