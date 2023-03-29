Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 col : COLOR0;
                float4 vertex : SV_POSITION;
            };

            struct particleData
            {
                float3 pos;
                float4 color;
            };

            StructuredBuffer<particleData> _particleDataBuffer;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                // o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex = UnityObjectToClipPos(float4(_particleDataBuffer[id].pos,0));
                o.col = _particleDataBuffer[id].color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.col;
            }
            ENDCG
        }
    }
}
