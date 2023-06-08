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
        public void DrawHyperToonLogo()
        {
            titleStyle = new GUIStyle();
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.BoldAndItalic;
            titleStyle.normal.textColor = new Color(.7f, .7f, .7f);
            
            subtitleStyle = new GUIStyle();
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
    }
    
    public class MainShaderGUI : ShaderGUI
    {
        private HyperToonEditor e;

        private bool edgeConstantsFoldout;
        private readonly Vector2 edgeConstantsScrollPos = new(7, 13);
        private bool rimFoldout;
        private readonly Vector2 rimScrollPos = new(13, 17);
        private bool halftoneFoldout;
        private readonly Vector2 halftoneScrollPos = new(17, 22);
        private bool reflectionFoldout;
        private readonly Vector2 reflectionScrollPos = new(22, 24);

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
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
            // Lighting
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            // Foldouts
            // Edge Constants
            edgeConstantsFoldout = e.DrawRangeFoldout(edgeConstantsFoldout, "Edge Constants",
                edgeConstantsScrollPos);
            // Rim
            rimFoldout = e.DrawRangeFoldout(rimFoldout, "Rim", rimScrollPos);
            // Halftone
            halftoneFoldout = e.DrawRangeFoldout(halftoneFoldout, "Halftone", halftoneScrollPos);
            // Reflection
            reflectionFoldout = e.DrawRangeFoldout(reflectionFoldout, "Reflection", reflectionScrollPos);
            
            e.DrawAdvancedOptions();
        }
    }

    public class PatternShaderGUI : ShaderGUI
    {
        private HyperToonEditor e;

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
                MaterialProperty _WindowDimensions = FindProperty("_WindowDimensions", properties);
                _WindowDimensions.vectorValue = EditorGUILayout.Vector2Field("WindowDimensions", _WindowDimensions.vectorValue);
            }
            e.DrawToggleContent(_ToggleWindow, new []
            {
                FindProperty("_WindowOffset", properties),
                FindProperty("_WindowGutter", properties),
                FindProperty("_WindowRounding", properties),
                FindProperty("_RandomSeed", properties),
                FindProperty("_DarkPercentage", properties),
                FindProperty("_DarkNoiseScale", properties),
            });

            e.DrawAdvancedOptions();
        }
    }
}
