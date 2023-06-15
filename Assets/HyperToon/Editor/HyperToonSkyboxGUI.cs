using UnityEditor;
using UnityEngine;

namespace HyperToon
{
    public class HyperToonSkyboxGUI
    {
        
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
}