Shader "Unlit/radialGrid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineWidth("LineWidth", Range(0.0001, 0.0000001)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed _LineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = (v.uv * 2.0 - 1.0) * 1.01;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = length(i.uv.xy);

                float numRings = 5;
                float rings = abs(fmod(dist * (numRings * 2.0), 1) - 1.0);
                float alpha = smoothstep(0.96, 0.98, rings);

                // mask out rings outside of a radial distance of 1
                alpha *= 1.0 - saturate((dist - 1.005) * 100.0);
                
                return fixed4(1, 1, 1, alpha);
            }
            ENDCG
        }
    }
}
