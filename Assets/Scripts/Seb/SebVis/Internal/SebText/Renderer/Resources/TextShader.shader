Shader "Seb.Vis.Text/TextShader"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct InstanceData
            {
                float2 pos;
                float2 size;
                float4 col;
                float fontSize;
                int dataOffset;
                int groupIndex;
            };

            struct TextGroup
            {
                float2 offset;
                float2 maskMin;
                float2 maskMax;
                int useScreenSpace;
            };

            // Data stored for every instance (each individual glyph that needs to be rendered)
            StructuredBuffer<InstanceData> PerInstanceData;
            // Position data for the bezier curves
            StructuredBuffer<float2> BezierData;
            // Metadata for each glyph: bezier data offset, num contours, contour length/s
            StructuredBuffer<int> GlyphMetaData;
            // Info about each block of text being rendered: masks, position offset, etc
            StructuredBuffer<TextGroup> TextGroups;

            float4x4 WorldToClipSpace;
            float2 ScreenSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 pos : TEXCOORD0;
                float2 worldPos : TEXCOORD1; //
                int dataOffset : TEXCOORD2;
                float4 mask : TEXCOORD3;
                float4 col : TEXCOORD4;
            };

            float2 CalculateWorldUnitsPerPixel()
            {
                float2 screenSizeWorld = unity_OrthoParams.xy * 2; // world-space width & height of orthographic camera
                float2 screenSizePixels = _ScreenParams.xy; // Pixel size of screen
                float2 worldUnitsPerPixel = screenSizeWorld / screenSizePixels;
                return worldUnitsPerPixel;
            }

            float4 WorldToClipPos(float2 pos, bool useScreenSpace, float2 screenSize)
            {
                if (useScreenSpace)
                {
                    float2 uv = pos.xy / screenSize;
                    return float4(uv * 2 - 1, 0, 1);
                }
                float4 clipPos = mul(WorldToClipSpace, float4(pos, 0, 1.0));
                clipPos.y = clipPos.y * _ProjectionParams.x;
                #if UNITY_UV_STARTS_AT_TOP
                clipPos.y = -clipPos.y;
                #endif
                return clipPos;
            }

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                InstanceData instance = PerInstanceData[instanceID];
                TextGroup group = TextGroups[instance.groupIndex];

                v2f o;
                float2 aaPad = CalculateWorldUnitsPerPixel() * 2;
                float2 quadSize = instance.size + aaPad;

                float2 offset = instance.pos + group.offset;
                float2 worldVertPos = v.vertex * quadSize * instance.fontSize + offset;

                o.vertex = WorldToClipPos(worldVertPos, group.useScreenSpace, ScreenSize);

                o.pos = -quadSize / 2 + quadSize * v.uv;
                o.worldPos = worldVertPos;
                o.dataOffset = instance.dataOffset;
                o.mask = float4(group.maskMin, group.maskMax);
                o.col = instance.col;

                return o;
            }

            // Calculate roots of quadratic equation(value/s for which: a�t ^ 2 + b�t + c = 0)
            float2 CalculateQuadraticRoots(float a, float b, float c)
            {
                const float epsilon = 1e-5;
                float2 roots = -99999;

                // For a straight line, solve: b�t + c = 0; therefore t = -c/b
                if (abs(a) < epsilon)
                {
                    if (b != 0) roots[0] = -c / b;
                }
                else
                {
                    // Solve using quadratic formula: t = (-b � sqrt(b^2 - 4ac)) / (2a)
                    // If the value under the sqrt is negative, the equation has no real roots
                    float discriminant = b * b - 4 * a * c;

                    // Allow discriminant to be slightly negative to avoid a curve being missed due
                    // to precision limitations. Must be clamped to zero before it's used in sqrt though!
                    if (discriminant > -epsilon)
                    {
                        float s = sqrt(max(0, discriminant));
                        roots[0] = (-b + s) / (2 * a);
                        roots[1] = (-b - s) / (2 * a);
                    }
                }

                return roots;
            }

            // Calculate the fraction [0,1] of the pixel that is covered by the glyph (along the x axis).
            // This is done by looking at the distances to the intersection points of a horizontal ray
            // (at the pixel pos) with all the curves of the glyph.
            float CalculateHorizontalCoverage(float2 pixelPos, int dataOffset, float pixelSize)
            {
                float coverage = 0;
                float invPixelSize = 1 / pixelSize;

                int pointOffset = GlyphMetaData[dataOffset];
                int numContours = GlyphMetaData[dataOffset + 1];
                dataOffset += 2;

                // Loop over all contours
                for (int contourIndex = 0; contourIndex < numContours; contourIndex++)
                {
                    int numPoints = GlyphMetaData[dataOffset + contourIndex];

                    for (int i = 0; i < numPoints; i += 2)
                    {
                        // Get positions of curve's control points relative to the current pixel
                        float2 p0 = BezierData[i + 0 + pointOffset] - pixelPos;
                        float2 p1 = BezierData[i + 1 + pointOffset] - pixelPos;
                        float2 p2 = BezierData[i + 2 + pointOffset] - pixelPos;

                        // Check if curve segment is going downwards (this means that a ray crossing
                        // it from left to right would be exiting the shape at this point).
                        // Note: curves are assumed to be monotonic (strictly increasing or decreasing on the y axis)
                        bool isDownwardCurve = p0.y > 0 || p2.y < 0;

                        // Skip curves that are entirely above or below the ray
                        // When two curves are in the same direction (upward or downward), only one of them should be
                        // counted at their meeting point to avoid double-counting. When in opposite directions, however,
                        // the curve is not crossing the contour (but rather just grazing it) and so the curves should
                        // either both be skipped, or both counted (so as not to affect the end result).
                        if (isDownwardCurve)
                        {
                            if (p0.y < 0 && p2.y <= 0) continue;
                            if (p0.y > 0 && p2.y >= 0) continue;
                        }
                        else
                        {
                            if (p0.y <= 0 && p2.y < 0) continue;
                            if (p0.y >= 0 && p2.y > 0) continue;
                        }

                        // Calculate a,b,c of quadratic equation for current bezier curve
                        float2 a = p0 - 2 * p1 + p2;
                        float2 b = 2 * (p1 - p0);
                        float2 c = p0;

                        // Calculate roots to see if ray intersects curve segment.
                        // Note: intersection is allowed slightly outside of [0, 1] segment to tolerate precision issues.
                        const float epsilon = 1e-4;
                        float2 roots = CalculateQuadraticRoots(a.y, b.y, c.y);
                        bool onSeg0 = roots[0] >= -epsilon && roots[0] <= 1 + epsilon;
                        bool onSeg1 = roots[1] >= -epsilon && roots[1] <= 1 + epsilon;

                        // Calculate distance to intersection (negative if to left of ray)
                        float t0 = saturate(roots[0]);
                        float t1 = saturate(roots[1]);
                        float intersect0 = a.x * t0 * t0 + b.x * t0 + c.x;
                        float intersect1 = a.x * t1 * t1 + b.x * t1 + c.x;

                        // Calculate the fraction of the ray that passes through the glyph (within the current pixel):
                        // A value [0, 1] is calculated based on where the intersection occurs: 0 at the left edge of
                        // the pixel, increasing to 1 at the right edge. This value is added to the total coverage
                        // value when the ray exits a shape, and subtracted when the ray enters a shape.
                        int sign = isDownwardCurve ? 1 : -1;
                        if (onSeg0) coverage += saturate(0.5 + intersect0 * invPixelSize) * sign;
                        if (onSeg1) coverage += saturate(0.5 + intersect1 * invPixelSize) * sign;
                    }

                    pointOffset += numPoints + 1;
                }

                return saturate(coverage);
            }

            // Run for every pixel in the glyph's quad mesh
            float4 frag(v2f input) : SV_Target
            {
                // Mask
                float2 worldPos = input.worldPos;
                float2 maskMin = input.mask.xy;
                float2 maskMax = input.mask.zw;
                if (worldPos.x < maskMin.x || worldPos.x > maskMax.x || worldPos.y < maskMin.y || worldPos.y > maskMax.y)
                {
                    return 0;
                }

                // Size of pixel in glyph space
                float pixelSize = ddx(input.pos.x);
                float alphaSum = 0;

                // Render 3 times (with slight y offset) for anti-aliasing
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    float2 samplePos = input.pos + float2(0, yOffset) * pixelSize / 3.0;
                    float coverage = CalculateHorizontalCoverage(samplePos, input.dataOffset, pixelSize);
                    alphaSum += coverage;
                }

                float alpha = input.col.a * alphaSum / 3.0;
                //alpha = max(0.3, alpha);
                return float4(input.col.rgb, alpha);
            }
            ENDCG
        }
    }
}