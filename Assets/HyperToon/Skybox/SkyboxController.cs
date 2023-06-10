using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HyperToon
{
    [ExecuteAlways]
    public class SkyboxController : MonoBehaviour
    {
        [SerializeField] private Light directionalLight;
        [SerializeField] private float sunSetThresholdAngle = 70;
        [SerializeField] private float sunSetLeewayAngle = 30;
        [SerializeField] Transform sun;
        [SerializeField] Transform moon;
        private float intensityMultiplier;

        void LateUpdate()
        {
            // Sun
            Shader.SetGlobalVector("_SunDir", -sun.forward);
            // Moon
            Shader.SetGlobalVector("_MoonDir", -moon.forward);
            Shader.SetGlobalMatrix("_MoonSpaceMatrix", new Matrix4x4(-moon.forward, 
                -moon.up, -moon.right, Vector4.zero).transpose);
            
            MatchLighting();
        }

        void MatchLighting()
        {
            // angle < 90 means below horizon
            float currentSunAngle = Vector3.Angle(Vector3.up, sun.forward);
            float t = (currentSunAngle - sunSetThresholdAngle) / sunSetLeewayAngle;

            directionalLight.intensity = Mathf.Lerp(.1f, 1, t);
            if (directionalLight.intensity < .2f && Vector3.Angle(Vector3.up, moon.forward) > 90)
                directionalLight.transform.rotation = moon.rotation;
            else
                directionalLight.transform.rotation = sun.rotation;
            
            // moon.intensity = Mathf.Lerp(.5f, 0, t);
        }
    }
}
