using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HyperToon
{
    public class HyperToonEditor
    {
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        
        private readonly MaterialEditor materialEditor;
        private readonly MaterialProperty[] properties;
        
        public HyperToonEditor(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.materialEditor = materialEditor;
            this.properties = properties;
            DrawHyperToonLogo();
        }
        
        /// <summary>
        /// Draw HyperToon branded logo on top of the inspector.
        /// </summary>
        public static void DrawHyperToonLogo()
        {
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.BoldAndItalic;
            titleStyle.normal.textColor = new Color(.7f, .7f, .7f);
            
            GUIStyle subtitleStyle = new GUIStyle();
            subtitleStyle.fontSize = 11;
            subtitleStyle.fontStyle = FontStyle.Italic;
            subtitleStyle.normal.textColor = Color.gray;
            
            EditorGUILayout.LabelField("HyperToon", titleStyle);
            EditorGUILayout.LabelField(HyperToonInfo.FullInfo, subtitleStyle);
            GUILayout.Space(20);
        }

        /// <summary>
        /// Draw Unity advanced options at the end.
        /// </summary>
        public void DrawAdvancedOptions()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="foldout"></param>
        /// <param name="displayName"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool DrawRangeFoldout(bool foldout, string displayName, Vector2 range)
        {
            GUILayout.Space(3);
            foldout = EditorGUILayout.Foldout(foldout, displayName, EditorStyles.foldoutHeader);
            if (foldout)
            {
                for (int i = (int)range.x; i < range.y; i++)
                {
                    materialEditor.ShaderProperty(properties[i], properties[i].displayName);
                }
            }
            return foldout;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="booleanProperty"></param>
        /// <param name="propertiesToToggle"></param>
        public void DrawToggleContent(MaterialProperty booleanProperty, MaterialProperty[] propertiesToToggle)
        {
            if (booleanProperty.floatValue == 1)
            {
                foreach (MaterialProperty p in propertiesToToggle)
                {
                    materialEditor.ShaderProperty(p, p.displayName);
                }
            }
        }

        public void DrawCastShadows(MaterialProperty castShadowProperty)
        {
            // Cast Shadows
            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(castShadowProperty);
            bool newValue = EditorGUILayout.Toggle("Cast Shadows", castShadowProperty.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                castShadowProperty.floatValue = newValue ? 1.0f : 0.0f;
            MaterialEditor.EndProperty();
            Material mat = materialEditor.target as Material;
            mat.SetShaderPassEnabled("ShadowCaster", newValue);
        }
    }
    
    public class MainShaderGUI : BaseShaderGUI
    {
        private HyperToonEditor e;

        private bool edgeConstantsFoldout = true;
        private readonly Vector2 edgeConstantsScrollPos = new(7, 13);
        private bool rimFoldout = true;
        private readonly Vector2 rimScrollPos = new(13, 17);
        private bool halftoneFoldout = true;
        private readonly Vector2 halftoneScrollPos = new(17, 22);
        private bool reflectionFoldout = true;
        private readonly Vector2 reflectionScrollPos = new(22, 24);
        private readonly Vector2 transparentScrollPos = new(25, 29);
        private bool shadowFoldout = true;
        private readonly Vector2 shadowScrollPos = new(29, 30);

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // base.OnGUI(materialEditor, properties);
            e = new HyperToonEditor(materialEditor, properties);
            
            

            // Base
            EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
            MaterialProperty _Color = FindProperty("_Color", properties);
            materialEditor.ShaderProperty(_Color, _Color.displayName);
            MaterialProperty _MainTexture = FindProperty("_MainTexture", properties);
            materialEditor.ShaderProperty(_MainTexture, "Main Texture");
            // Smoothness
            materialEditor.ShaderProperty(FindProperty("_Smoothness", properties),
                FindProperty("_Smoothness", properties).displayName);
            materialEditor.ShaderProperty(FindProperty("_SmoothnessMap", properties),
                FindProperty("_SmoothnessMap", properties).displayName);
            // Normals
            MaterialProperty _UseNormals = FindProperty("_UseCustomNormals", properties);
            materialEditor.ShaderProperty(_UseNormals, "Use Custom Normals");
            e.DrawToggleContent(_UseNormals, new [] {FindProperty("_NormalMap", properties), FindProperty("_NormalStrength", properties)});
            // Transparency
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Transparency", EditorStyles.boldLabel);
            GUILayout.Label("Note: Assign the correct transparent layer to object,\nand add in the correct render feature in renderer.");
            MaterialProperty _IsTransparent = FindProperty("_IsTransparent", properties);
            materialEditor.ShaderProperty(_IsTransparent, "Is Transparent");
            if (_IsTransparent.floatValue == 1)
            {
                for (int i = (int)transparentScrollPos.x; i < transparentScrollPos.y; i++)
                {
                    MaterialProperty p = properties[i];
                    materialEditor.ShaderProperty(p, p.displayName);
                }
            }
            // Lighting foldouts
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            // Edge Constants
            edgeConstantsFoldout = e.DrawRangeFoldout(edgeConstantsFoldout, "Edge Constants",
                edgeConstantsScrollPos);
            // Rim
            rimFoldout = e.DrawRangeFoldout(rimFoldout, "Rim", rimScrollPos);
            // Halftone
            halftoneFoldout = e.DrawRangeFoldout(halftoneFoldout, "Halftone", halftoneScrollPos);
            // Reflection
            reflectionFoldout = e.DrawRangeFoldout(reflectionFoldout, "Reflection", reflectionScrollPos);
            // Shadows
            shadowFoldout = e.DrawRangeFoldout(shadowFoldout, "Shadow", shadowScrollPos);
            
            // Cast Shadows (Surface Options)
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Surface Options", EditorStyles.boldLabel);
            e.DrawCastShadows(FindProperty("_CastShadows", properties, false));

            // Advanced Options
            e.DrawAdvancedOptions();
        }

        public void ValidateMaterial(Material material)
        {

        }
    }

    public class PatternShaderGUI : ShaderGUI
    {
        private HyperToonEditor e;

        private bool windowShapeFoldout;
        private bool windowRandomnessFoldout;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            e = new HyperToonEditor(materialEditor, properties);

            // Gradient
            EditorGUILayout.LabelField("Gradient", EditorStyles.boldLabel);
            MaterialProperty _Color1 = FindProperty("_Color1", properties);
            MaterialProperty _Color2 = FindProperty("_Color2", properties);
            MaterialProperty _GradientOffset = FindProperty("_GradientOffset", properties);
            materialEditor.ShaderProperty(_Color1, _Color1.displayName);
            materialEditor.ShaderProperty(_Color2, _Color2.displayName);
            materialEditor.ShaderProperty(_GradientOffset, _GradientOffset.displayName);
            
            MaterialProperty _AutoGradientRotation = FindProperty("_AutoGradientRotation", properties);
            materialEditor.ShaderProperty(_AutoGradientRotation, "Time Based Auto Rotation");
            e.DrawToggleContent(_AutoGradientRotation, new []
            {
                FindProperty("_ManualGradientRotation", properties)
            });
            // Dithering
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Halftone Dithering", EditorStyles.boldLabel);
            MaterialProperty _DotDensity = FindProperty("_DotDensity", properties);
            MaterialProperty _DotRotation = FindProperty("_DotRotation", properties);
            MaterialProperty _DotSize = FindProperty("_DotSize", properties);
            materialEditor.ShaderProperty(_DotDensity, _DotDensity.displayName);
            materialEditor.ShaderProperty(_DotRotation, _DotRotation.displayName);
            materialEditor.ShaderProperty(_DotSize, _DotSize.displayName);
            // Window toggle
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Window Mode", EditorStyles.boldLabel);
            MaterialProperty _ToggleWindow = FindProperty("_ToggleWindow", properties);
            materialEditor.ShaderProperty(_ToggleWindow, "Toggle Window Mode");
            if (_ToggleWindow.floatValue == 1)
            {
                GUILayout.Space(3);
                windowShapeFoldout = EditorGUILayout.Foldout(windowShapeFoldout, "Window Shape", EditorStyles.foldoutHeader);
                if (windowShapeFoldout)
                {
                    MaterialProperty _WindowDimensions = FindProperty("_WindowDimensions", properties);
                    _WindowDimensions.vectorValue = EditorGUILayout.Vector2Field("WindowDimensions", _WindowDimensions.vectorValue);
                    for (int i = 10; i <13; i++)
                    {
                        materialEditor.ShaderProperty(properties[i], properties[i].displayName);
                    }
                }
                windowRandomnessFoldout = e.DrawRangeFoldout(windowRandomnessFoldout, "Randomness", new Vector2(13, 16));
            }

            e.DrawAdvancedOptions();
        }
    }
}
