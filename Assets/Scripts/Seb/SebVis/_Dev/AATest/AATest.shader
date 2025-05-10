Shader "Unlit/AATest"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 test : TEXCOORD1;
                float4 test2 : TEXCOORD2;
                float2 worldPos : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex.xy = v.uv * 2 - 1;
                o.test = o.vertex;
                o.test2 = float4(v.uv * 2 - 1, 0, 1);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;

                o.uv = v.uv;
                return o;
            }

            float2 pos;
            float2 size;

            float circleSdf(float2 p, float2 centre, float rad)
            {
                return length(p - centre) - rad;
            }

            float boxSdf(float2 p, float2 centre, float2 size)
            {
                float2 offset = abs(p - centre) - size;
                float unsignedDst = length(max(offset, 0));
                float dstInsideBox = max(min(offset.x, 0), min(offset.y, 0));
                return unsignedDst + dstInsideBox;
            }

            float4 frag(v2f i) : SV_Target
            {
                // return length(i.worldPos-0) - 3;
                //float sdf = boxSdf(i.worldPos, pos, size);
                //return float4(0,  sdf/4, sdf < 0, 0);
                return float4(i.uv * 0.5, 0, 0);
                return i.vertex.x < 8;
                return 1;
            }
            ENDCG
        }
    }
}