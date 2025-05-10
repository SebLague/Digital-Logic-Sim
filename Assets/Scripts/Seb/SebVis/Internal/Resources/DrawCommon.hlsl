float4x4 WorldToClipSpace;
float2 ScreenSize;
int useScreenSpace;

float4 WorldToClipPos(float3 worldPos)
{
    if (useScreenSpace)
    {
        float2 uv = worldPos.xy / ScreenSize;
        return float4(uv * 2 - 1, 0, 1);
    }
    float4 clipPos = mul(WorldToClipSpace, float4(worldPos, 1.0));
    clipPos.y = clipPos.y * _ProjectionParams.x;
    #if UNITY_UV_STARTS_AT_TOP
    clipPos.y = -clipPos.y;
    #endif
    return clipPos;
}

float boxSdf(float2 p, float2 centre, float2 size)
{
    float2 offset = abs(p - centre) - size;
    float unsignedDst = length(max(offset, 0));
    float dstInsideBox = max(min(offset.x, 0), min(offset.y, 0));
    return unsignedDst + dstInsideBox;
}

float3 rgb_to_hsv(float3 c)
{
    // Thanks to http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
    float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 hsv_to_rgb(float3 c)
{
    // Thanks to http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}
