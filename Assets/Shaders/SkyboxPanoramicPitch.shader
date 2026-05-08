// Cubemap skybox + Y rotation + pitch (tilt). Dusk.exr in this project imports as CUBE, not 2D lat-long.
Shader "Custom/SkyboxCubemapPitch"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Horizontal Rotation", Range(0, 360)) = 0
        _Pitch ("Pitch Up (deg)", Range(-45, 45)) = 12
        [NoScaleOffset] _MainTex ("Cubemap (HDR)", Cube) = "" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            samplerCUBE _MainTex;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            float _Pitch;

            float3 RotateAroundYInDegrees(float3 v, float degrees)
            {
                float a = degrees * UNITY_PI / 180.0;
                float s, c;
                sincos(a, s, c);
                float2x2 m = float2x2(c, -s, s, c);
                return float3(mul(m, v.xz), v.y).xzy;
            }

            float3 RotateAroundXInDegrees(float3 v, float degrees)
            {
                float a = degrees * UNITY_PI / 180.0;
                float s, c;
                sincos(a, s, c);
                float2x2 m = float2x2(c, -s, s, c);
                float2 yz = mul(m, v.yz);
                return float3(v.x, yz.x, yz.y);
            }

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                // Match Unity Skybox/Cubemap: Y rotation affects clip-space footprint and ray basis.
                float3 dir = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(dir);
                o.dir = dir;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                // Pitch only in fragment so it tilts the dome without breaking cube projection.
                d = RotateAroundXInDegrees(d, _Pitch);
                half4 tex = texCUBE(_MainTex, d);
                half3 c = tex.rgb * _Tint.rgb * _Exposure;
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
