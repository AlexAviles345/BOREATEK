Shader "Boreatek/PolarSky"
{
    Properties
    {
        _Night ("Polar night", Range(0, 1)) = 0
        _Storm ("Snowstorm", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Night;
                float _Storm;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash(float2 coordinate)
            {
                return frac(sin(dot(coordinate, float2(112.1, 271.9))) * 43758.5453);
            }

            float AuroraCurtain(float azimuth, float elevation, float time, float offset)
            {
                float ribbon = 0.21 + 0.075 * sin(2.0 * azimuth + offset + time * 0.08)
                    + 0.030 * sin(6.0 * azimuth - 1.7 * offset);
                float height = elevation - ribbon;
                float trailingEdge = smoothstep(-0.02, 0.02, height) * exp(-7.6 * max(0.0, height));
                float fibers = saturate(0.58 + 0.22 * sin(94.0 * azimuth + height * 7.0 + offset)
                    + 0.23 * sin(179.0 * azimuth + offset * 4.0 + time * 0.15));
                return trailingEdge * fibers * smoothstep(-0.38, 0.24, sin(azimuth + offset));
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float height = saturate(direction.y);
                float highSky = smoothstep(0.0, 0.79, height);
                float3 daySky = lerp(float3(0.52, 0.71, 0.81), float3(0.12, 0.35, 0.56), highSky);
                float3 nightSky = lerp(float3(0.036, 0.096, 0.149), float3(0.009, 0.022, 0.058), highSky);
                float3 sky = lerp(daySky, nightSky, _Night);
                float azimuth = atan2(direction.x, direction.z);

                float2 starCell = float2(azimuth * 84.0, direction.y * 167.0);
                float star = step(0.989, Hash(floor(starCell))) * (1.0 - smoothstep(0.07, 0.25, length(frac(starCell) - 0.5)));
                star *= smoothstep(0.025, 0.24, height);

                float time = _Time.y;
                float green = AuroraCurtain(azimuth, height, time, 0.0);
                float violet = AuroraCurtain(azimuth, height - 0.075, time, 2.3);
                float3 aurora = green * float3(0.027, 0.49, 0.29) + violet * float3(0.12, 0.22, 0.46);
                sky += (aurora + star * 0.72) * _Night * (1.0 - _Storm);

                float3 stormSky = lerp(float3(0.37, 0.51, 0.62), float3(0.18, 0.27, 0.37), highSky);
                stormSky *= lerp(1.0, 0.60, _Night);
                return half4(lerp(sky, stormSky, _Storm), 1.0);
            }
            ENDHLSL
        }
    }
}
