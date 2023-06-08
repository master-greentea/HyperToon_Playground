// Based on cel shading from Robin Seibold (https://www.youtube.com/watch?v=gw31oF9qITw)

#ifndef HYPERTOON_LIGHTING_INCLUDED
#define HYPERTOON_LIGHTING_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// structs
// edge settings
struct EdgeConstants
{
    float diffuse;
    float specular;
    float specularBoost;
    float specularOffset;
    float distanceAttenuation;
    float shadowAttenuation;
    float rim;
    float rimSoftness;
    bool useHalftoneShadow;
    bool useHalftoneHighlight;
    float voroniHalftone;
};
// general surface variables
struct SurfaceVariables
{
    float3 albedo;
    float3 normal;
    float3 view;
    float smoothness;
    float shininess;
    float rimThreshold;
    float3 rimColor;
    float fogFactor;
    EdgeConstants edge;
};

float3 CalculateCelShading(Light l, SurfaceVariables s, bool isMainLight)
{
    // attenuation
    float shadowAttentuationSmoothstepped = smoothstep(0, s.edge.shadowAttenuation, l.shadowAttenuation);
    float distanceAttenuationSmoothstepped = smoothstep(0, s.edge.distanceAttenuation, l.distanceAttenuation);
    float attenuation = shadowAttentuationSmoothstepped * distanceAttenuationSmoothstepped;
    // diffuse
    float diffuse = saturate(dot(s.normal, l.direction));
    diffuse *= attenuation;
    // specular
    float specular = saturate(dot(s.normal, normalize(l.direction + s.view)));
    specular = pow(specular, s.shininess);
    specular *= diffuse * s.smoothness;
    // rim reflection
    float rim = 1 - dot(s.view, s.normal);
    rim *= pow(diffuse, s.rimThreshold);

    // cel shading
    diffuse = smoothstep(
        0,
        s.edge.useHalftoneShadow ? s.edge.diffuse * s.edge.voroniHalftone : s.edge.diffuse,
        diffuse); // diffuse with halftone (for shadow dithering)
    specular = s.smoothness * smoothstep(
        (1 - s.smoothness) * s.edge.specular + s.edge.specularOffset,
        s.edge.specular + s.edge.specularOffset,
        s.edge.useHalftoneHighlight ? specular * s.edge.voroniHalftone : specular); // specular with halftone
    rim = s.smoothness * smoothstep(
        s.edge.rim - .5 * s.edge.rimSoftness,
        s.edge.rim + .5 * s.edge.rimSoftness,
        s.edge.useHalftoneHighlight ? rim * s.edge.voroniHalftone : rim); // rim highlight with halftone

    // ==TESTING==
    // return smoothstep(0, 1, diffuse);
    
    // only sample light colors of additional light (temporary)
    // specular boost if is calculating main light
    float3 color = (isMainLight ? s.albedo * l.color : l.color) * (diffuse + max(specular, rim))
        + (isMainLight ? s.edge.specularBoost : 0) * max(specular, rim * s.rimColor);

    // mix fog
    color = MixFog(color, s.fogFactor);
    
    return color;
}
#endif

void HyperToonLighting_float(float3 Albedo, float Smoothness, float RimThreshold,
    float3 Position, float3 Normal, float3 View,
    float EdgeDiffuse, float EdgeSpecular, float EdgeSpecularBoost, float EdgeSpecularOffset,
    float EdgeDistanceAttenuation, float EdgeShadowAttenuation,
    float EdgeRim, float EdgeRimSoftness, float3 RimColor,
    bool UseHalftoneShadows, bool UseHalftoneHightlights, float VoroniHalftone,
    out float3 Color)
{
#ifdef SHADERGRAPH_PREVIEW
    // Shader graph preview
    Color = float3(.5, .5, .5);
#else
    // Assigning data to structs
    // Edge constants
    EdgeConstants e;
    e.diffuse = EdgeDiffuse;
    e.specular = EdgeSpecular;
    e.specularBoost = EdgeSpecularBoost;
    e.specularOffset = EdgeSpecularOffset;
    e.distanceAttenuation = EdgeDistanceAttenuation;
    e.shadowAttenuation = EdgeShadowAttenuation;
    e.rim = EdgeRim;
    e.rimSoftness = EdgeRimSoftness;
    e.useHalftoneShadow = UseHalftoneShadows;
    e.useHalftoneHighlight = UseHalftoneHightlights;
    e.voroniHalftone = VoroniHalftone;
    // Surface variables
    SurfaceVariables s;
    s.albedo = Albedo;
    s.normal = normalize(Normal);
    s.view = SafeNormalize(View);
    s.smoothness = Smoothness;
    s.shininess = exp2(10 * Smoothness + 1);
    s.rimThreshold = RimThreshold;
    s.rimColor = RimColor;
    s.edge = e;
    // fog
    s.fogFactor = ComputeFogFactor(TransformWorldToHClip(Position).z);

    // Shadow calculations
    #ifdef SHADOWS_SCREEN
        float4 clipPos = TransformWorldToHClip(Position);
        float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(Position);
    #endif

    // Calculate main light
    Light light = GetMainLight(shadowCoord);
    Color = CalculateCelShading(light, s, true);

    // Calculate additional lights
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        light = GetAdditionalLight(i, Position, 1);
        Color += CalculateCelShading(light, s, false);
    }
#endif
}
#endif
