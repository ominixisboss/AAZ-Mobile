#ifndef AAZ_HD2D_SPRITE_INPUT_INCLUDED
#define AAZ_HD2D_SPRITE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half4  _BaseColor;
    half4  _EmissionColor;
    half4  _ShadowTint;
    half4  _RimColor;
    half   _Cutoff;
    half   _NormalStrength;
    half   _NormalBend;
    half   _LightWrap;
    half   _AmbientBoost;
    half   _RimPower;
    half   _VerticalTilt;
    half   _ShadowStrength;
CBUFFER_END

TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);

// Set every frame by HD2DCameraDirector. We deliberately do NOT use
// _WorldSpaceCameraPos: during the shadow pass URP swaps it for the light's
// position, which would twist every billboard and produce shadows that do not
// match the silhouette the player sees.
float4 _HD2DCameraPosition;
float4 _HD2DCameraUp;

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
    float4 color      : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS  : SV_POSITION;
    float2 uv          : TEXCOORD0;
    float3 positionWS  : TEXCOORD1;
    float3 normalWS    : TEXCOORD2;
    float3 tangentWS   : TEXCOORD3;
    float3 bitangentWS : TEXCOORD4;
    half4  color       : TEXCOORD5;
    half   fogFactor   : TEXCOORD6;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD7;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// Builds a camera-facing basis for this quad and places the vertex on it.
// The quad's pivot (object-space origin) stays put, so a sprite standing on the
// ground keeps its feet planted while the card spins to face the camera.
void HD2DBuildBillboard(float3 positionOS, out float3 positionWS,
                        out float3 normalWS, out float3 tangentWS, out float3 bitangentWS)
{
    float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

#if defined(_BILLBOARD_NONE)
    positionWS  = TransformObjectToWorld(positionOS);
    normalWS    = TransformObjectToWorldDir(float3(0.0, 0.0, -1.0));
    tangentWS   = TransformObjectToWorldDir(float3(1.0, 0.0, 0.0));
    bitangentWS = cross(normalWS, tangentWS);
#else
    float3 toCam = _HD2DCameraPosition.xyz - originWS;

    #if defined(_BILLBOARD_YAXIS)
        // Yaw-only: the sprite never leans, so vertical pixel columns stay vertical.
        toCam.y = 0.0;
    #endif

    // Camera sitting exactly on the pivot would give us a zero-length forward.
    if (dot(toCam, toCam) < 1e-8)
        toCam = float3(0.0, 0.0, -1.0);

    float3 fwd = normalize(toCam);

    float3 up = float3(0.0, 1.0, 0.0);
    #if defined(_BILLBOARD_FULL)
        up = normalize(_HD2DCameraUp.xyz);
    #endif

    float3 right = cross(up, fwd);
    if (dot(right, right) < 1e-8)
        right = float3(1.0, 0.0, 0.0);
    right = normalize(right);
    up = cross(fwd, right);

    // Lean the card away from the camera. A few degrees stops the top edge from
    // clipping through low ceilings and lets overhead lights graze the sprite.
    half t = radians(_VerticalTilt);
    half st, ct;
    sincos(t, st, ct);
    float3 tiltedUp  = up * ct - fwd * st;
    float3 tiltedFwd = fwd * ct + up * st;

    // Preserve non-uniform scale from the transform.
    float3x3 m = (float3x3)GetObjectToWorldMatrix();
    float scaleX = length(float3(m[0][0], m[1][0], m[2][0]));
    float scaleY = length(float3(m[0][1], m[1][1], m[2][1]));

    positionWS  = originWS + right * (positionOS.x * scaleX) + tiltedUp * (positionOS.y * scaleY);
    normalWS    = tiltedFwd;
    tangentWS   = right;
    bitangentWS = tiltedUp;
#endif
}

Varyings HD2DVertex(Attributes IN)
{
    Varyings OUT = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    float3 positionWS, normalWS, tangentWS, bitangentWS;
    HD2DBuildBillboard(IN.positionOS.xyz, positionWS, normalWS, tangentWS, bitangentWS);

    OUT.positionWS  = positionWS;
    OUT.normalWS    = normalWS;
    OUT.tangentWS   = tangentWS;
    OUT.bitangentWS = bitangentWS;
    OUT.positionCS  = TransformWorldToHClip(positionWS);
    OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
    OUT.color       = IN.color;
    OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    OUT.shadowCoord = TransformWorldToShadowCoord(positionWS);
#endif
    return OUT;
}

half4 HD2DSampleAlbedo(Varyings IN)
{
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor * IN.color;
    clip(c.a - _Cutoff);
    return c;
}

// Flat cards light badly: every pixel shares one normal, so a sprite is either
// fully lit or fully dark. Bending the normal across the sprite fakes the
// curvature of a cylinder and gives a soft terminator down the body.
float3 HD2DShadingNormal(Varyings IN)
{
    float3 n = normalize(IN.normalWS);
    float3 t = normalize(IN.tangentWS);
    float3 b = normalize(IN.bitangentWS);

    n = normalize(n + t * ((IN.uv.x - 0.5) * 2.0 * _NormalBend));

#if defined(_NORMALMAP)
    half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _NormalStrength);
    n = normalize(t * nTS.x + b * nTS.y + n * nTS.z);
#endif
    return n;
}

// Wrapped (half-Lambert style) diffuse. Pure Lambert makes pixel-art sprites
// read as cut-out cardboard because half the sprite goes black; wrapping the
// terminator around keeps readable colour in shadow, which is the whole point
// of hand-authored sprite art.
half HD2DWrapDiffuse(float3 normalWS, float3 lightDirWS)
{
    half ndl = dot(normalWS, lightDirWS);
    return saturate((ndl + _LightWrap) / (1.0h + _LightWrap));
}

float4 HD2DShadowCoord(Varyings IN)
{
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    return IN.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    return TransformWorldToShadowCoord(IN.positionWS);
#else
    return float4(0.0, 0.0, 0.0, 0.0);
#endif
}

#endif // AAZ_HD2D_SPRITE_INPUT_INCLUDED
