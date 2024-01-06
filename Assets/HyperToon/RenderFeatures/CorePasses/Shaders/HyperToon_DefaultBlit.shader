Shader "HyperToon/RenderFeatures/HyperToon_DefaultBlit"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            sampler2D _MainTex;

            fixed4 frag (v2f_img i) : SV_Target
            {
            	fixed4 col = tex2D(_MainTex, i.uv);
				return col;
            }
            ENDCG
        }
    }
}