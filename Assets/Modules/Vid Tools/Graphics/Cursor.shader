Shader "Custom/Cursor"{
	Properties{
		_Color ("Tint", Color) = (0, 0, 0, 1)
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader{
		Tags{ 
			"RenderType"="Transparent" 
			"Queue"="Transparent"
		}

		Blend SrcAlpha OneMinusSrcAlpha//

		ZWrite off
		Cull off

		Pass{

			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			fixed4 _Color;

			struct appdata{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			v2f vert(appdata v){
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}

			float4 CubicHermite (float4 A, float4 B, float4 C, float4 D, float4 t)
			{
				float t2 = t*t;
				float t3 = t*t*t;
				float4 a = -A/2.0 + (3.0*B)/2.0 - (3.0*C)/2.0 + D/2.0;
				float4 b = A - (5.0*B)/2.0 + 2.0*C - D / 2.0;
				float4 c = -A/2.0 + C/2.0;
				float4 d = B;
				
				return a*t3 + b*t2 + c*t + d;
			}

			float4 BicubicHermitetex2DSample (float2 uv)
			{
				float2 textureSize = _MainTex_TexelSize.zw;
				float2 d = 1.0 / textureSize;
		
				float2 pixel = uv * textureSize + 0.5;
				
				float2 frac = pixel - floor(pixel);
				pixel = floor(pixel) / textureSize - d/2.0;
				
				float4 C00 = tex2D(_MainTex, pixel + float2(-d.x, -d.y));
				float4 C10 = tex2D(_MainTex, pixel + float2(0, -d.y));
				float4 C20 = tex2D(_MainTex, pixel + float2(d.x, -d.y));
				float4 C30 = tex2D(_MainTex, pixel + float2(d.x * 2, -d.y));
				
				float4 C01 = tex2D(_MainTex, pixel + float2(-d.x, 0));
				float4 C11 = tex2D(_MainTex, pixel + float2(0, 0));
				float4 C21 = tex2D(_MainTex, pixel + float2(d.x, 0));
				float4 C31 = tex2D(_MainTex, pixel + float2(d.x * 2, 0.0));
				
				float4 C02 = tex2D(_MainTex, pixel + float2(-d.x, d.y));
				float4 C12 = tex2D(_MainTex, pixel + float2(0, d.y));
				float4 C22 = tex2D(_MainTex, pixel + float2(d.x, d.y));
				float4 C32 = tex2D(_MainTex, pixel + float2(d.x * 2, d.y));
				
				float4 C03 = tex2D(_MainTex, pixel + float2(-d.x, d.y * 2));
				float4 C13 = tex2D(_MainTex, pixel + float2(0, d.y * 2));
				float4 C23 = tex2D(_MainTex, pixel + float2(d.x, d.y * 2));
				float4 C33 = tex2D(_MainTex, pixel + float2(d.x * 2, d.y * 2));
				
				float4 CP0X = CubicHermite(C00, C10, C20, C30, frac.x);
				float4 CP1X = CubicHermite(C01, C11, C21, C31, frac.x);
				float4 CP2X = CubicHermite(C02, C12, C22, C32, frac.x);
				float4 CP3X = CubicHermite(C03, C13, C23, C33, frac.x);
				
				return CubicHermite(CP0X, CP1X, CP2X, CP3X, frac.y);
			}


			fixed4 frag(v2f i) : SV_TARGET{
				fixed4 col = BicubicHermitetex2DSample(i.uv);
				col *= _Color;
				col *= i.color;
				return col;
			}

			ENDCG
		}
	}
}