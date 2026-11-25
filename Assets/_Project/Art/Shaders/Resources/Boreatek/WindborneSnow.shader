Shader "Boreatek/WindborneSnow"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.color = input.color;
                output.uv = input.uv;
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                half nearFade = smoothstep(0.8, 2.5, input.eyeDepth);
                half alpha = input.color.a * nearFade * (1.0 - smoothstep(0.26, 1.0, radius));
                return half4(input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
