Shader "HyperToon/Skybox/HyperToon_Skybox"
{
    Properties
    {
        [NoScaleOffset] _SunZenithGrad ("Sun-Zenith gradient", 2D) = "white" {}
        [NoScaleOffset] _ViewZenithGrad ("View-Zenith gradient", 2D) = "white" {}
        [NoScaleOffset] _SunViewGrad ("Sun-View gradient", 2D) = "white" {}
        // Sun
        _SunRadius ("Sun radius", Range(0, 1)) = 0.05
        _SunIntensity ("Sun intensity", Range(1, 3)) = 1
        [MaterialToggle] _SynthSun ("Synth sun", Float) = 0
        _SynthSunLines ("Synth sun lines", Range(0, 1)) = 0.5
        _SynthSunBottom ("Synth sun bottom", Range(0, 1)) = 0.5
        // Moon
        [NoScaleOffset] _MoonCubeMap ("Moon cube map", Cube) = "black" {}
        [MaterialToggle] _MoonOn("Moon On", Float) = 1
        _MoonRadius ("Moon radius", Range(0, 1)) = 0.05
        _MoonEdgeStrength ("Moon edge strength", Range(0.01, 1)) = 0.5
        _MoonExposure ("Moon exposure", Range(-16, 0)) = 0
        _MoonDarkside ("Moon darkside", Range(0, 1)) = 0.5
        // Day
        [NoScaleOffset] _CloudGrad ("Cloud color gradient", 2D) = "white" {}
        [NoScaleOffset] _CloudCubeMap ("Cloud cube map", Cube) = "black" {}
        [MaterialToggle] _CloudOn("Cloud On", Float) = 1
        _CloudSpeed ("Cloud speed", Float) = 0.001
        [NoScaleOffset] _CloudBackCubeMap ("Cloud cube map", Cube) = "black" {}
        _Cloudiness ("Cloudiness", Range(0, 1)) = 0.5
        // Night
        [NoScaleOffset] _StarCubeMap ("Star cube map", Cube) = "black" {}
        _StarExposure ("Star exposure", Range(-16, 16)) = 0
        _StarPower ("Star power", Range(1, 5)) = 1
        _StarLatitude ("Star latitude", Range(-90, 90)) = 0
        _StarSpeed ("Star speed", Float) = 0.001
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SunZenithGrad);
            SAMPLER(sampler_SunZenithGrad);
            TEXTURE2D(_ViewZenithGrad);
            SAMPLER(sampler_ViewZenithGrad);
            TEXTURE2D(_SunViewGrad);
            SAMPLER(sampler_SunViewGrad);
            TEXTURE2D(_CloudGrad);
            SAMPLER(sampler_CloudGrad);

            TEXTURECUBE(_MoonCubeMap);
            SAMPLER(sampler_MoonCubeMap);
            TEXTURECUBE(_StarCubeMap);
            SAMPLER(sampler_StarCubeMap);
            TEXTURECUBE(_CloudCubeMap);
            SAMPLER(sampler_CloudCubeMap);
            TEXTURECUBE(_CloudBackCubeMap);
            SAMPLER(sampler_CloudBackCubeMap);

            struct Attributes
            {
                float4 posOS    : POSITION;
                float2 uv       : TEXCOORD1;
            };

            struct Varyings
            {
                float4 posCS        : SV_POSITION;
                float3 viewDirWS    : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };

            Varyings Vertex(Attributes v)
            {
                Varyings o = (Varyings)0;
    
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.posOS.xyz);
    
                o.posCS = vertexInput.positionCS;
                o.viewDirWS = vertexInput.positionWS;
                o.uv = v.uv;

                return o;
            }

            float3 _SunDir;
            float _SunIntensity;
            float _SunRadius;
            float _SynthSun, _SynthSunLines, _SynthSunBottom;
            
            float3 _MoonDir;
            float _MoonOn;
            float _MoonRadius;
            float _MoonEdgeStrength;
            float _MoonExposure;
            float _MoonDarkside;
            float4x4 _MoonSpaceMatrix;

            float _StarExposure, _StarPower;
            float _StarLatitude, _StarSpeed;

            float _CloudSpeed, _CloudOn, _Cloudiness;

            float GetSunMask(float sunViewDot, float sunRadius)
            {
                float stepRadius = 1 - sunRadius * sunRadius;
                return step(stepRadius, sunViewDot);
            }

            float SphereIntersect(float3 rayDir, float3 spherePos, float radius)
            {
                float3 oc = -spherePos;
                float b = dot(oc, rayDir);
                float c = dot(oc, oc) - radius * radius;
                float h = b * b - c;
                if(h < 0.0) return -1.0;
                h = sqrt(h);
                return -b - h;
            }

            float3 GetMoonTexture(float3 normal)
            {
                float3 uvw = mul(_MoonSpaceMatrix, float4(normal,0)).xyz;
                float3x3 correctionMatrix = float3x3(0, -0.2588190451, -0.9659258263,
                    0.08715574275, 0.9622501869, -0.2578341605,
                    0.9961946981, -0.08418598283, 0.02255756611);
                uvw = mul(correctionMatrix, uvw);
                
                return SAMPLE_TEXTURECUBE(_MoonCubeMap, sampler_MoonCubeMap, uvw).rgb;
            }

            float3x3 AngleAxis3x3(float angle, float3 axis)
            {
                float c, s;
                sincos(angle, s, c);

                float t = 1 - c;
                float x = axis.x;
                float y = axis.y;
                float z = axis.z;

                return float3x3(
                    t * x * x + c, t * x * y - s * z, t * x * z + s * y,
                    t * x * y + s * z, t * y * y + c, t * y * z - s * x,
                    t * x * z - s * y, t * y * z + s * x, t * z * z + c
                    );
            }

            float3 GetStarUVW(float3 viewDir, float latitude, float localSiderealTime)
            {
                // tilt = 0 at the north pole, where latitude = 90 degrees
                float tilt = PI * (latitude - 90) / 180;
                float3x3 tiltRotation = AngleAxis3x3(tilt, float3(1,0,0));

                // 0.75 is a texture offset for lST = 0 equals noon
                float spin = (0.75-localSiderealTime) * 2 * PI;
                float3x3 spinRotation = AngleAxis3x3(spin, float3(0, 1, 0));
                
                // The order of rotation is important
                float3x3 fullRotation = mul(spinRotation, tiltRotation);

                return mul(fullRotation,  viewDir);
            }

            float4 Fragment(Varyings v) : SV_TARGET
            {
                float3 viewDir = normalize(v.viewDirWS);

                // angles
                float sunViewDot = dot(_SunDir, viewDir);
                float sunZenithDot = _SunDir.y;
                float viewZenithDot = viewDir.y;
                float sunMoonDot = dot(_SunDir, _MoonDir);

                float sunViewDot1 = (sunViewDot + 1) * 0.5;
                float sunZenithDot1 = (sunZenithDot + 1) * 0.5;

                // sky colors
                float3 sunZenithColor = SAMPLE_TEXTURE2D(_SunZenithGrad, sampler_SunZenithGrad, float2(sunZenithDot1, 0.5)).rgb;
                float3 viewZenithColor = SAMPLE_TEXTURE2D(_ViewZenithGrad, sampler_ViewZenithGrad, float2(sunZenithDot1, 0.5)).rgb;
                float vzMask = pow(saturate(1.0 - viewZenithDot), 4);
                float3 sunViewColor = SAMPLE_TEXTURE2D(_SunViewGrad, sampler_SunViewGrad, float2(sunZenithDot1, 0.5)).rgb;
                float svMask = pow(saturate(sunViewDot), 4);

                float3 skyColor = sunZenithColor + vzMask * viewZenithColor + svMask * sunViewColor;
                
                // The sun
                float sunMask = GetSunMask(sunViewDot, _SunRadius);
                float bottom = step(1 + lerp(-.3, .3, _SynthSunBottom) - sunZenithDot, 1 - v.uv.g) * smoothstep(.7, .75, 1 - sunZenithDot);
                float smoothBottom = smoothstep(1 - sunZenithDot, 2 - sunZenithDot, 1 - v.uv.g);
                float lines = lerp(_SynthSunLines, _SynthSunLines * 5, smoothBottom);
                sunMask = _SynthSun < 1 ? sunMask : 
                    saturate(
                    step(.5, tan(v.uv.g * lines + 1))
                    ) * bottom * sunMask + (1 - bottom) * sunMask;
                float3 sunColor = _MainLightColor.rgb * sunMask;

                // The moon
                float moonIntersect = SphereIntersect(viewDir, _MoonDir, _MoonRadius);
                float moonMask = moonIntersect > -1 ? 1 : 0;
                float3 moonNormal = normalize(_MoonDir - viewDir * moonIntersect);
                float moonNdotL = saturate(dot(moonNormal, -_SunDir));
                float3 moonTexture = GetMoonTexture(moonNormal);
                float3 moonColor = moonMask * moonNdotL * exp2(_MoonExposure);
                moonColor = smoothstep(0, _MoonEdgeStrength, moonColor) * moonTexture;
                moonColor += moonMask * saturate(_MoonDarkside * moonTexture);
                moonColor *= _MoonOn;

                // clouds
                float3 cloudUVW = GetStarUVW(viewDir, 90, _Time.y * _CloudSpeed % 1);
                float3 cloudColor = SAMPLE_TEXTURECUBE_BIAS(_CloudCubeMap, sampler_CloudCubeMap, cloudUVW, -1).rgb;
                cloudColor *= _CloudOn;
                // clouds back
                float3 cloudBackUVW = GetStarUVW(viewDir, 90, _Time.y * (_CloudSpeed / 4) % 1);
                float3 cloudBackColor = SAMPLE_TEXTURECUBE_BIAS(_CloudBackCubeMap, sampler_CloudBackCubeMap, cloudBackUVW, -1).rgb;
                cloudBackColor *= _Cloudiness * _CloudOn;
                skyColor *= lerp(1, 0.7, _Cloudiness * _CloudOn); // darken color for when cloudy

                // stars
                float3 starUVW = GetStarUVW(viewDir, _StarLatitude, _Time.y * _StarSpeed % 1);
                float3 starColor = SAMPLE_TEXTURECUBE_BIAS(_StarCubeMap, sampler_StarCubeMap, starUVW, -1).rgb;
                starColor = pow(abs(starColor), _StarPower);
                float starStrength = (1 - sunViewDot1) * saturate(-sunZenithDot);
                starColor *= (1 - sunMask) * (1 - moonMask) * exp2(_StarExposure) * starStrength;
                
                // Solar eclipse
                sunColor *= 1 - moonMask;
                float solarEclipse01 = smoothstep(1 - _SunRadius * _SunRadius, 1.0, sunMoonDot);
                skyColor *= lerp(1, 0.3, solarEclipse01); // darken color for when eclipsed
                sunColor *= (1 - moonMask) * lerp(1, 4, solarEclipse01); // increase sun brightness for when eclipsed
                sunColor *= _SunIntensity;

                // Lunar eclipse
                float lunarEclipseMask = 1 - step(1 - _SunRadius * _SunRadius, -sunViewDot);
                float lunarEclipse01 = smoothstep(1 - _SunRadius * _SunRadius * 0.05, 1.0, -sunMoonDot);
                moonColor *= lerp(lunarEclipseMask, float3(0.4, 0.05, 0), lunarEclipse01);

                // clouds block sun, moon, stars
                // cloud blocking
                float3 cloudBlocking = 1 - smoothstep(0.01, .1, cloudColor + cloudBackColor);
                sunColor = sunColor * cloudBlocking;
                moonColor = moonColor * cloudBlocking;
                starColor = starColor * cloudBlocking;
                // darken clouds for night
                float3 cloudRawColor = SAMPLE_TEXTURE2D(_CloudGrad, sampler_CloudGrad, float2(sunZenithDot1, 0.5)).rgb;
                float3 cloudColoring = (1 - starStrength) * cloudRawColor;
                cloudColor *= cloudColoring;
                cloudBackColor *= cloudColoring;
                cloudBackColor *= pow(saturate(sunViewDot), 6) + cloudBackColor;
                

                float3 col = skyColor + sunColor + cloudBackColor + cloudColor + starColor + moonColor;

                // col = smoothstep(.7, .8, 1 - sunZenithDot);
                
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}
