Shader "Boreatek/SnowFootprints"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" "RenderType" = "Transparent" }
        Pass
        {
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float fog : TEXCOORD1; };

            #pragma multi_compile_fog
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float x = uv.x - 0.49 + (uv.y - 0.45) * 0.14;
                float sole = length(float2(x / 0.43, (uv.y - 0.66) / 0.33));
                float heel = length(float2((x - 0.075) / 0.31, (uv.y - 0.24) / 0.21));
                float edge = min(sole, heel);
                float softEdge = max(0.045, fwidth(edge));
                float shape = 1.0 - smoothstep(0.98 - softEdge, 1.0, edge);
                float tread = 0.73 + 0.27 * smoothstep(0.13, 0.28, frac(uv.y * 10 + abs(x) * 1.65));
                input.color.rgb = MixFog(input.color.rgb, input.fog);
                input.color.a *= shape * tread;
                return input.color;
            }
            ENDHLSL
        }
    }
}
