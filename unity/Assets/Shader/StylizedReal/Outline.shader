// Screen-space outline as a URP Renderer Feature pass.
// Attach OutlineRendererFeature (see Script/Graphics) to the URP Renderer asset.
Shader "StylizedReal/Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.1, 0.1, 0.1, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.005
        _DepthThreshold ("Depth Threshold", Range(0, 1)) = 0.02
        _NormalThreshold ("Normal Threshold", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            // Unity 6 URP: Blit.hlsl が提供する Vert を使用（旧 FullscreenVert は廃止）
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D_X(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _DepthThreshold;
                float  _NormalThreshold;
            CBUFFER_END

            half SampleDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
            }

            half3 SampleNormal(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv).rgb;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 texelSize = _OutlineWidth * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                // Sample 4 neighbors for edge detection
                float2 offsets[4] = {
                    float2( texelSize.x, 0),
                    float2(-texelSize.x, 0),
                    float2(0,  texelSize.y),
                    float2(0, -texelSize.y)
                };

                half  depthCenter  = SampleDepth(uv);
                half3 normalCenter = SampleNormal(uv);

                half depthEdge  = 0;
                half normalEdge = 0;
                for (int i = 0; i < 4; i++)
                {
                    half  d = SampleDepth(uv + offsets[i]);
                    half3 n = SampleNormal(uv + offsets[i]);
                    depthEdge  += abs(depthCenter - d);
                    normalEdge += distance(normalCenter, n);
                }

                half outline = step(_DepthThreshold, depthEdge)
                             + step(_NormalThreshold, normalEdge);
                outline = saturate(outline);

                // Unity 6: LOAD_TEXTURE2D_X は整数座標を要求するため SAMPLE に変更
                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                return lerp(src, _OutlineColor, outline);
            }
            ENDHLSL
        }
    }
}
