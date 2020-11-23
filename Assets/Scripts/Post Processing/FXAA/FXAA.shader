Shader "Hidden/FXAA" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		float _ContrastThreshold, _RelativeThreshold;
		float _SubpixelBlending;

		struct VertexData {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Interpolators {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		Interpolators VertexProgram (VertexData v) {
			Interpolators i;
			i.pos = UnityObjectToClipPos(v.vertex);
			i.uv = v.uv;
			return i;
		}

		float4 Sample (float2 uv) {
			return tex2Dlod(_MainTex, float4(uv, 0, 0));
		}

		float SampleLuminance (float2 uv) {
			#if defined(LUMINANCE_GREEN)
				return Sample(uv).g;
			#else
				return Sample(uv).a;
			#endif
		}

		float SampleLuminance (float2 uv, float uOffset, float vOffset) {
			uv += _MainTex_TexelSize * float2(uOffset, vOffset);
			return SampleLuminance(uv);
		}

		struct LuminanceData {
			float m, n, e, s, w;
			float ne, nw, se, sw;
			float highest, lowest, contrast;
		};

		LuminanceData SampleLuminanceNeighborhood (float2 uv) {
			LuminanceData l;
			l.m = SampleLuminance(uv);
			l.n = SampleLuminance(uv,  0,  1);
			l.e = SampleLuminance(uv,  1,  0);
			l.s = SampleLuminance(uv,  0, -1);
			l.w = SampleLuminance(uv, -1,  0);

			l.ne = SampleLuminance(uv,  1,  1);
			l.nw = SampleLuminance(uv, -1,  1);
			l.se = SampleLuminance(uv,  1, -1);
			l.sw = SampleLuminance(uv, -1, -1);

			l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
			l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
			l.contrast = l.highest - l.lowest;
			return l;
		}

		bool ShouldSkipPixel (LuminanceData l) {
			float threshold =
				max(_ContrastThreshold, _RelativeThreshold * l.highest);
			return l.contrast < threshold;
		}

		float DeterminePixelBlendFactor (LuminanceData l) {
			float filter = 2 * (l.n + l.e + l.s + l.w);
			filter += l.ne + l.nw + l.se + l.sw;
			filter *= 1.0 / 12;
			filter = abs(filter - l.m);
			filter = saturate(filter / l.contrast);

			float blendFactor = smoothstep(0, 1, filter);
			return blendFactor * blendFactor * _SubpixelBlending;
		}

		struct EdgeData {
			bool isHorizontal;
			float pixelStep;
			float oppositeLuminance, gradient;
		};

		EdgeData DetermineEdge (LuminanceData l) {
			EdgeData e;
			float horizontal =
				abs(l.n + l.s - 2 * l.m) * 2 +
				abs(l.ne + l.se - 2 * l.e) +
				abs(l.nw + l.sw - 2 * l.w);
			float vertical =
				abs(l.e + l.w - 2 * l.m) * 2 +
				abs(l.ne + l.nw - 2 * l.n) +
				abs(l.se + l.sw - 2 * l.s);
			e.isHorizontal = horizontal >= vertical;

			float pLuminance = e.isHorizontal ? l.n : l.e;
			float nLuminance = e.isHorizontal ? l.s : l.w;
			float pGradient = abs(pLuminance - l.m);
			float nGradient = abs(nLuminance - l.m);

			e.pixelStep =
				e.isHorizontal ? _MainTex_TexelSize.y : _MainTex_TexelSize.x;
			
			if (pGradient < nGradient) {
				e.pixelStep = -e.pixelStep;
				e.oppositeLuminance = nLuminance;
				e.gradient = nGradient;
			}
			else {
				e.oppositeLuminance = pLuminance;
				e.gradient = pGradient;
			}

			return e;
		}

		#if defined(LOW_QUALITY)
			#define EDGE_STEP_COUNT 4
			#define EDGE_STEPS 1, 1.5, 2, 4
			#define EDGE_GUESS 12
		#else
			#define EDGE_STEP_COUNT 10
			#define EDGE_STEPS 1, 1.5, 2, 2, 2, 2, 2, 2, 2, 4
			#define EDGE_GUESS 8
		#endif

		static const float edgeSteps[EDGE_STEP_COUNT] = { EDGE_STEPS };

		float DetermineEdgeBlendFactor (LuminanceData l, EdgeData e, float2 uv) {
			float2 uvEdge = uv;
			float2 edgeStep;
			if (e.isHorizontal) {
				uvEdge.y += e.pixelStep * 0.5;
				edgeStep = float2(_MainTex_TexelSize.x, 0);
			}
			else {
				uvEdge.x += e.pixelStep * 0.5;
				edgeStep = float2(0, _MainTex_TexelSize.y);
			}

			float edgeLuminance = (l.m + e.oppositeLuminance) * 0.5;
			float gradientThreshold = e.gradient * 0.25;

			float2 puv = uvEdge + edgeStep * edgeSteps[0];
			float pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
			bool pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;

			UNITY_UNROLL
			for (int i = 1; i < EDGE_STEP_COUNT && !pAtEnd; i++) {
				puv += edgeStep * edgeSteps[i];
				pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
				pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
			}
			if (!pAtEnd) {
				puv += edgeStep * EDGE_GUESS;
			}

			float2 nuv = uvEdge - edgeStep * edgeSteps[0];
			float nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
			bool nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;

			UNITY_UNROLL
			for (int i = 1; i < EDGE_STEP_COUNT && !nAtEnd; i++) {
				nuv -= edgeStep * edgeSteps[i];
				nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
				nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
			}
			if (!nAtEnd) {
				nuv -= edgeStep * EDGE_GUESS;
			}

			float pDistance, nDistance;
			if (e.isHorizontal) {
				pDistance = puv.x - uv.x;
				nDistance = uv.x - nuv.x;
			}
			else {
				pDistance = puv.y - uv.y;
				nDistance = uv.y - nuv.y;
			}

			float shortestDistance;
			bool deltaSign;
			if (pDistance <= nDistance) {
				shortestDistance = pDistance;
				deltaSign = pLuminanceDelta >= 0;
			}
			else {
				shortestDistance = nDistance;
				deltaSign = nLuminanceDelta >= 0;
			}

			if (deltaSign == (l.m - edgeLuminance >= 0)) {
				return 0;
			}
			return 0.5 - shortestDistance / (pDistance + nDistance);
		}

		float4 ApplyFXAA (float2 uv) {
			LuminanceData l = SampleLuminanceNeighborhood(uv);
			if (ShouldSkipPixel(l)) {
				return Sample(uv);
			}

			float pixelBlend = DeterminePixelBlendFactor(l);
			EdgeData e = DetermineEdge(l);
			float edgeBlend = DetermineEdgeBlendFactor(l, e, uv);
			float finalBlend = max(pixelBlend, edgeBlend);

			if (e.isHorizontal) {
				uv.y += e.pixelStep * finalBlend;
			}
			else {
				uv.x += e.pixelStep * finalBlend;
			}
			return float4(Sample(uv).rgb, l.m);
		}
	ENDCG

	SubShader {
		Cull Off
		ZTest Always
		ZWrite Off

		Pass { // 0 luminancePass
			CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram

				#pragma multi_compile _ GAMMA_BLENDING

				float4 FragmentProgram (Interpolators i) : SV_Target {
					float4 sample = tex2D(_MainTex, i.uv);
					sample.rgb = saturate(sample.rgb);
					sample.a = LinearRgbToLuminance(sample.rgb);
					#if defined(GAMMA_BLENDING)
						sample.rgb = LinearToGammaSpace(sample.rgb);
					#endif
					return sample;
				}
			ENDCG
		}

		Pass { // 1 fxaaPass
			CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram

				#pragma multi_compile _ LUMINANCE_GREEN
				#pragma multi_compile _ LOW_QUALITY
				#pragma multi_compile _ GAMMA_BLENDING

				float4 FragmentProgram (Interpolators i) : SV_Target {
					float4 sample = ApplyFXAA(i.uv);
					#if defined(GAMMA_BLENDING)
						sample.rgb = GammaToLinearSpace(sample.rgb);
					#endif
					return sample;
				}
			ENDCG
		}
	}
}