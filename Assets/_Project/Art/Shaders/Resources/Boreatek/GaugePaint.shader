Shader "Boreatek/GaugePaint"
{
    Properties
    {
        [MainColor] _BaseColor("Paint", Color) = (0.5, 0.5, 0.5, 1)
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            float4 Vertex(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            half4 Fragment() : SV_Target
            {
                // Keep the camera-facing markings below the HDR bloom range.
                return half4(saturate(_BaseColor.rgb), 1.0h);
            }
            ENDHLSL
        }
    }
}
