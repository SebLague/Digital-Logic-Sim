Shader "Vis/Draw"
{
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
        }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "DrawCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 posClip : SV_POSITION;
                int shapeType : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float2 sizeData : TEXCOORD2;
                float4 col : TEXCOORD3;
                float2 worldPos :TEXCOORD4;
                float4 lineEndPoints :TEXCOORD5;
                float2 posLocal :TEXCOORD6;
                float4 maskMinMax :TEXCOORD7;
                float invTexelSize : TEXCOORD8;
                bool useAntialiasing : TEXCOORD9;
            };

            struct ShapeData
            {
                int type; // shape type
                float2 a; // arbitrary data A (depends on shape type)
                float2 b; // arbitrary data B (depends on shape type)
                float param;
                float4 col;
                float4 maskMinMax;
            };

            float2 LayerOffset;
            float LayerScale;

            StructuredBuffer<ShapeData> InstanceData;
            uint InstanceOffset;

            static const int LINE_TYPE = 0;
            static const int POINT_TYPE = 1;
            static const int QUAD_TYPE = 2;
            static const int TRIANGLE_TYPE = 3;
            static const int SATVAL_TYPE = 4;
            static const int HUE_TYPE = 5;
            static const int DIAMOND_TYPE = 6;
            static const int POINT_OUTLINE_TYPE = 7;

            static const bool AA_Enabled = true;

            // Calculate size of a texel in current space (screen or orthographic-world)
            // * Screenspace: vertex positions are supplied directly as texel coordinates.
            //   This means that a 1x1 quad covers exactly 1 texel (and so the size of a texel in this space is just 1).
            // * World (orthographic): vertex positions are supplied in 'world space'. To convert to texel coordinates, the vertex
            //   positions have to be divided by the camera's orthoSize, and scaled up by the texel size of the screen/renderTarget.
            //   This means that a 1x1 quad has a size (in texels) of screenSizeTexel / camOrthoSize.
            //   And so the size of a single texel in this space is the inverse of that: camOrthoSize / screenSizeTexel
            // Note: this function returns both width and height of texel, but they should typically be the same...
            float2 CalculateTexelSize()
            {
                if (useScreenSpace) return 1;

                float2 camOrthoSize = unity_OrthoParams.xy * 2; // world-space width & height of orthographic camera
                float2 screenSizeTexel = _ScreenParams.xy; // Texel size of render target
                return camOrthoSize / screenSizeTexel; // Size of a single texel (i.e how many world units it covers based on cam's current ortho size)
            }

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                ShapeData instance = InstanceData[instanceID + InstanceOffset];
                v2f o;
                o.shapeType = instance.type;
                o.maskMinMax = instance.maskMinMax;
                o.col = instance.col;
                o.uv = v.uv;
                float3 worldVertPos = 0;

                // currently disabling AA for screenspace quads due to it causing annoying slivers between elements (todo: fix?)
                bool useAntialiasing = AA_Enabled && (!useScreenSpace || instance.type != QUAD_TYPE);
                o.useAntialiasing = useAntialiasing;

                // Shapes should be slightly expanded for anti-aliasing
                float2 texelSize = CalculateTexelSize();
                float2 antialisingPadding = useAntialiasing ? texelSize * 2 : 0;
                o.invTexelSize = 1.0 / texelSize.x;

                // Line
                if (instance.type == LINE_TYPE)
                {
                    float2 worldPosA = instance.a * LayerScale + LayerOffset;
                    float2 worldPosB = instance.b * LayerScale + LayerOffset;
                    float2 centre = (worldPosA + worldPosB) * 0.5;

                    // Calculate line direction and perp direction
                    float2 lineOffset = worldPosB - worldPosA;
                    float lineSegmentLength = length(lineOffset);
                    float2 lineDir = lineOffset / lineSegmentLength;
                    float2 linePerpDir = float2(-lineDir.y, lineDir.x);

                    // Calculate vertex pos (quad needs to be extended by thickness to allow for rounded endpoints)
                    float thickness = abs(instance.param * LayerScale);
                    float2 linePadAA = v.vertex.xy * 2 * antialisingPadding; // v.vertex * 2 gives offset direction
                    float lineMeshLength = lineSegmentLength + thickness * 2;
                    float2 vertOffsetLocal = v.vertex.xy * float2(lineMeshLength, thickness * 2) + linePadAA;
                    float2 vertOffsetWorld = lineDir * vertOffsetLocal.x + linePerpDir * vertOffsetLocal.y;
                    worldVertPos = float3(centre + vertOffsetWorld, 0);

                    o.sizeData = float2(lineSegmentLength, thickness);
                    o.lineEndPoints = float4(worldPosA, worldPosB);
                }
                // Point
                else if (instance.type == POINT_TYPE || instance.type == POINT_OUTLINE_TYPE)
                {
                    float2 radius = instance.b.xy;

                    float2 worldCentre = float2(instance.a * LayerScale + LayerOffset);
                    float2 size = radius * 2 * LayerScale;
                    float2 vertexLocal = v.vertex.xy * (size + antialisingPadding);
                    worldVertPos = float3(worldCentre + vertexLocal, 0);
                    o.posLocal = vertexLocal;
                    o.sizeData = radius;

                    if (instance.type == POINT_OUTLINE_TYPE)
                    {
                        o.sizeData = instance.param; // inner radius (as value between 0 and 1)
                    }
                }
                // Quad (2 = regular, 4 = saturation/value display, 5 = hue display)
                else if (instance.type == QUAD_TYPE || instance.type == SATVAL_TYPE || instance.type == HUE_TYPE)
                {
                    float3 worldCentre = float3(instance.a * LayerScale + LayerOffset, 0);
                    float4 size = float4(instance.b.xy * LayerScale, 1, 1);
                    float2 vertexLocal = v.vertex.xy * (size + antialisingPadding);
                    worldVertPos = float3(worldCentre + vertexLocal, 0);
                    o.posLocal = vertexLocal;
                    o.sizeData = instance.b.xy;

                    if (instance.type == 4) o.col = instance.param; // Override col to store hue value
                }
                // Triangle
                else if (instance.type == TRIANGLE_TYPE)
                {
                    uint uintInput = asuint(instance.param);
                    float cx = f16tof32(uintInput >> 16);
                    float cy = f16tof32(uintInput);

                    float2 posA = instance.a * LayerScale + LayerOffset;
                    float2 posB = instance.b * LayerScale + LayerOffset;
                    float2 posC = float2(cx, cy) * LayerScale + LayerOffset;
                    float2 pos = lerp(lerp(posA, posB, v.uv[1]), posC, v.uv[0]);
                    worldVertPos = float3(pos, 0);
                }
                // Diamond
                else if (instance.type == DIAMOND_TYPE)
                {
                    float3 worldCentre = float3(instance.a * LayerScale + LayerOffset, 0);
                    float2 size = instance.b.xy * LayerScale;
                    worldVertPos = float4(worldCentre + v.vertex * size, 0, 1);
                    o.sizeData = size;
                }

                o.worldPos = worldVertPos;
                o.posClip = WorldToClipPos(worldVertPos);
                return o;
            }

            bool inBounds(float2 pos, float2 boundsMin, float2 boundsMax)
            {
                return pos.x >= boundsMin && pos.x <= boundsMax.x && pos.y >= boundsMin.y && pos.y <= boundsMax.y;
            }

            float CalculateAlphaFromSDF(float sdf, v2f i)
            {
                if (!i.useAntialiasing) return 1;

                // -0.5 = texel fully inside shape; +0.5 = texel fully outside shape (well, approximately anyway...)
                float sdf_texel = sdf * i.invTexelSize;
                // Remap to [0, 1]
                float alpha = saturate(0.5 - sdf_texel);
                return alpha;
            }

            float distanceToLineSegment(float2 p, float2 a1, float2 a2)
            {
                float2 lineDelta = a2 - a1;
                float sqrLineLength = dot(lineDelta, lineDelta);

                if (sqrLineLength == 0)
                {
                    return a1;
                }

                float2 pointDelta = p - a1;
                float t = saturate(dot(pointDelta, lineDelta) / sqrLineLength);
                float2 pointOnLineSeg = a1 + lineDelta * t;
                return length(p - pointOnLineSeg);
            }

            // Alternate AA formulation which seems to work a bit better for very thin lines?
            float4 lineDraw_aa2(v2f i)
            {
                float pixelSize = ddx(i.worldPos.x);
                float thickness = i.sizeData.y;

                float2 offset = float2(0, 0) * pixelSize / 3;
                float2 samplePos = i.worldPos + offset;

                float dst = distanceToLineSegment(samplePos, i.lineEndPoints.xy, i.lineEndPoints.zw);
                float alpha = 1 - smoothstep(0, pixelSize, dst - thickness);

                return float4(i.col.rgb, i.col.a * alpha);
            }

            float4 lineDraw(v2f i)
            {
                float thickness = i.sizeData.y;

                float dst = distanceToLineSegment(i.worldPos, i.lineEndPoints.xy, i.lineEndPoints.zw);
                float sdf = dst - thickness;
                float alpha = CalculateAlphaFromSDF(sdf, i);

                return float4(i.col.rgb, i.col.a * alpha);
            }

            float4 circleDraw(v2f i)
            {
                float radius = i.sizeData.x;
                float sdf = length(i.posLocal) - radius;
                float alpha = CalculateAlphaFromSDF(sdf, i);
                return float4(i.col.rgb, alpha * i.col.a);
            }

            float4 circleOutlineDraw(v2f i)
            {
                float innerRadiusT = i.sizeData.x;

                // Calculate distance from centre of quad (dst > 1 is outside circle)
                float2 centreOffset = (i.uv.xy - 0.5) * 2;
                float sqrDst = dot(centreOffset, centreOffset);
                float dst = sqrt(sqrDst);

                // Smoothly blend from 0 to 1 alpha across edge of circle
                float delta = fwidth(dst);
                float alpha = max(dst - 1, innerRadiusT - dst);
                alpha = 1 - smoothstep(-delta, +delta, alpha);

                return float4(i.col.rgb, alpha * i.col.a);
            }


            float4 quadDraw(v2f i)
            {
                float sdf = boxSdf(0, i.posLocal, i.sizeData / 2);
                float alpha = CalculateAlphaFromSDF(sdf, i);

                float3 col = i.col.rgb;
                return float4(col, alpha * i.col.a);
            }

            float4 hueQuadDraw(v2f i)
            {
                float3 hsv = float3(i.uv.y, 1, 1);
                i.col = float4(hsv_to_rgb(hsv), 1);
                return quadDraw(i);
            }

            float4 satValQuadDraw(v2f i)
            {
                float hue = i.col[0];
                float3 hsv = float3(hue, i.uv[0], i.uv[1]);
                i.col = float4(hsv_to_rgb(hsv), 1);
                return quadDraw(i);
            }

            float4 diamondDraw(v2f i)
            {
                float2 size = i.sizeData;
                float2 p = abs(i.uv - 0.5) * size;

                if (size.x < size.y)
                {
                    size.xy = size.yx;
                    p.xy = p.yx;
                }

                p.x = size.x * 0.5 - p.x;
                float a = p.x > p.y;
                return float4(i.col.rgb, i.col.a * a);
            }


            float4 triangleDraw(v2f i)
            {
                return i.col;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Mask
                if (!inBounds(i.worldPos, i.maskMinMax.xy * LayerScale + LayerOffset, i.maskMinMax.zw * LayerScale + LayerOffset)) return 0;

                if (i.shapeType == LINE_TYPE) return lineDraw_aa2(i);
                if (i.shapeType == POINT_TYPE) return circleDraw(i);
                if (i.shapeType == POINT_OUTLINE_TYPE) return circleOutlineDraw(i);
                if (i.shapeType == QUAD_TYPE) return quadDraw(i);
                if (i.shapeType == TRIANGLE_TYPE) return triangleDraw(i);
                if (i.shapeType == SATVAL_TYPE) return satValQuadDraw(i);
                if (i.shapeType == HUE_TYPE) return hueQuadDraw(i);
                if (i.shapeType == DIAMOND_TYPE) return diamondDraw(i);
                return float4(0, 0, 0, 1);
            }
            ENDCG
        }
    }
}