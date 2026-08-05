Shader "PostProcess/RadialBlur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float   _Intensity;
        float   _CenterRadius;
        int     _SampleCount;
        float2  _BlurCenter ;    // 通常は（0.5, 0.5）
        ENDHLSL

        Pass
        {
            Name "RadialBlur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 dir = (_BlurCenter - uv);
                float2 d = dir;
                d.x *= _ScreenParams.x / _ScreenParams.y;
                float dist = length(d);
                float falloff = saturate((dist - _CenterRadius) / (1 - _CenterRadius));
                float2 delta = dir * _Intensity * falloff / _SampleCount;
                half4 color = 0;
                for (int i = 0; i < _SampleCount; i++)
                    color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + delta * i);
                color /= _SampleCount;
                return color;
            }
            ENDHLSL
        }
    }
}
