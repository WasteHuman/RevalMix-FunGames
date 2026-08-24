Shader "UI/Soft Glow"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        // ============================================================
        // GLOW
        // ============================================================

        [HDR]
        _GlowColor ("Glow Color", Color) = (1, 0.05, 0.02, 1)

        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.5

        // Размер внешнего свечения в пикселях.
        _GlowDistance ("Glow Distance", Range(0, 100)) = 20

        // Мягкость перехода.
        _GlowSoftness ("Glow Softness", Range(0.01, 1)) = 0.5

        // Как быстро glow затухает от края.
        _GlowFalloff ("Glow Falloff", Range(0.1, 8)) = 2

        // ============================================================
        // BLUR
        // ============================================================

        // Дополнительное размытие.
        _Blur ("Blur", Range(0, 1)) = 0.5

        // ============================================================
        // EDGE
        // ============================================================

        // Насколько ярким будет непосредственно край.
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1

        // Ширина яркой линии непосредственно возле спрайта.
        _EdgeWidth ("Edge Width", Range(0.1, 10)) = 2

        // ============================================================
        // PULSE
        // ============================================================

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1

        // 0 = не пульсирует
        // 1 = от 0 до 1
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0

        // ============================================================
        // NOISE
        // ============================================================

        [Toggle(_NOISE_ON)]
        _UseNoise ("Use Noise", Float) = 0

        [NoScaleOffset]
        _NoiseTex ("Noise Texture", 2D) = "gray" {}

        _NoiseScale ("Noise Scale", Range(0.1, 20)) = 4

        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1

        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.2

        // ============================================================
        // UI
        // ============================================================

        [HideInInspector]
        _StencilComp ("Stencil Comparison", Float) = 8

        [HideInInspector]
        _Stencil ("Stencil ID", Float) = 0

        [HideInInspector]
        _StencilOp ("Stencil Operation", Float) = 0

        [HideInInspector]
        _StencilWriteMask ("Stencil Write Mask", Float) = 255

        [HideInInspector]
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInInspector]
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "SoftGlow"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma target 3.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _NOISE_ON

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // ========================================================
            // STRUCTS
            // ========================================================

            struct Attributes
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex        : SV_POSITION;
                float4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            // ========================================================
            // TEXTURES
            // ========================================================

            sampler2D _MainTex;
            sampler2D _NoiseTex;

            float4 _MainTex_TexelSize;

            // ========================================================
            // UI
            // ========================================================

            float4 _ClipRect;

            // ========================================================
            // PARAMETERS
            // ========================================================

            float4 _Color;

            float4 _GlowColor;

            float _GlowIntensity;
            float _GlowDistance;
            float _GlowSoftness;
            float _GlowFalloff;

            float _Blur;

            float _EdgeIntensity;
            float _EdgeWidth;

            float _PulseSpeed;
            float _PulseAmount;

            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseSpeed;

            // ========================================================
            // VERTEX
            // ========================================================

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.vertex =
                    UnityObjectToClipPos(input.vertex);

                output.uv =
                    input.uv;

                output.color =
                    input.color * _Color;

                output.worldPosition =
                    input.vertex;

                return output;
            }

            // ========================================================
            // SAMPLE ALPHA
            // ========================================================

            float SampleAlpha(float2 uv)
            {
                return tex2D(
                    _MainTex,
                    uv
                ).a;
            }

            // ========================================================
            // GLOW SAMPLE
            //
            // Несколько колец вокруг текущего пикселя.
            //
            // Благодаря этому glow имеет:
            //
            //   яркий центр
            //        ↓
            //   мягкое размытие
            //        ↓
            //   плавное затухание
            //
            // ========================================================

            float CalculateBlurredAlpha(float2 uv)
            {
                float2 texel =
                    _MainTex_TexelSize.xy;

                float distance =
                    _GlowDistance;

                float blur =
                    lerp(
                        0.5,
                        1.5,
                        _Blur
                    );

                float result = 0.0;

                float weight = 0.0;

                // ----------------------------------------------------
                // RING 1
                // ----------------------------------------------------

                float radius1 =
                    distance * 0.20 * blur;

                float weight1 =
                    1.0;

                result += SampleAlpha(
                    uv + float2( 1,  0) * texel * radius1
                ) * weight1;

                result += SampleAlpha(
                    uv + float2(-1,  0) * texel * radius1
                ) * weight1;

                result += SampleAlpha(
                    uv + float2( 0,  1) * texel * radius1
                ) * weight1;

                result += SampleAlpha(
                    uv + float2( 0, -1) * texel * radius1
                ) * weight1;

                weight += weight1 * 4.0;

                // ----------------------------------------------------
                // RING 2
                // ----------------------------------------------------

                float radius2 =
                    distance * 0.45 * blur;

                float weight2 =
                    0.75;

                result += SampleAlpha(
                    uv + float2( 1,  0) * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + float2(-1,  0) * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + float2( 0,  1) * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + float2( 0, -1) * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + normalize(float2( 1,  1))
                    * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + normalize(float2(-1,  1))
                    * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + normalize(float2( 1, -1))
                    * texel * radius2
                ) * weight2;

                result += SampleAlpha(
                    uv + normalize(float2(-1,-1))
                    * texel * radius2
                ) * weight2;

                weight += weight2 * 8.0;

                // ----------------------------------------------------
                // RING 3
                // ----------------------------------------------------

                float radius3 =
                    distance * 0.70 * blur;

                float weight3 =
                    0.45;

                result += SampleAlpha(
                    uv + normalize(float2( 1,  0.4))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2(-1,  0.4))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2( 1, -0.4))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2(-1,-0.4))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2( 0.4,  1))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2(-0.4,  1))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2( 0.4, -1))
                    * texel * radius3
                ) * weight3;

                result += SampleAlpha(
                    uv + normalize(float2(-0.4,-1))
                    * texel * radius3
                ) * weight3;

                weight += weight3 * 8.0;

                // ----------------------------------------------------
                // RING 4
                // ----------------------------------------------------

                float radius4 =
                    distance * 1.0 * blur;

                float weight4 =
                    0.2;

                result += SampleAlpha(
                    uv + float2( 1,  0) * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + float2(-1,  0) * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + float2( 0,  1) * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + float2( 0, -1) * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + normalize(float2( 1,  1))
                    * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + normalize(float2(-1,  1))
                    * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + normalize(float2( 1, -1))
                    * texel * radius4
                ) * weight4;

                result += SampleAlpha(
                    uv + normalize(float2(-1,-1))
                    * texel * radius4
                ) * weight4;

                weight += weight4 * 8.0;

                return saturate(result / weight);
            }

            // ========================================================
            // EDGE GLOW
            // ========================================================

            float CalculateEdgeGlow(
                float originalAlpha,
                float blurredAlpha
            )
            {
                // Убираем glow изнутри спрайта.
                float outerGlow =
                    saturate(
                        blurredAlpha -
                        originalAlpha
                    );

                // Формируем яркую линию непосредственно возле края.
                float edge =
                    smoothstep(
                        0.0,
                        max(
                            0.001,
                            _EdgeWidth *
                            _MainTex_TexelSize.x
                        ),
                        outerGlow
                    );

                edge =
                    pow(
                        edge,
                        1.5
                    );

                return edge;
            }

            // ========================================================
            // PULSE
            // ========================================================

            float CalculatePulse()
            {
                float wave =
                    sin(
                        _Time.y *
                        _PulseSpeed
                    );

                wave =
                    wave * 0.5 +
                    0.5;

                return lerp(
                    1.0,
                    wave,
                    _PulseAmount
                );
            }

            // ========================================================
            // NOISE
            // ========================================================

            float CalculateNoise(float2 uv)
            {
                #if defined(_NOISE_ON)

                    float2 noiseUV =
                        uv *
                        _NoiseScale;

                    noiseUV +=
                        float2(
                            _Time.y * _NoiseSpeed,
                            _Time.y *
                            _NoiseSpeed *
                            0.73
                        );

                    float noise =
                        tex2D(
                            _NoiseTex,
                            noiseUV
                        ).r;

                    return lerp(
                        1.0 -
                        _NoiseStrength,

                        1.0,

                        noise
                    );

                #else

                    return 1.0;

                #endif
            }

            // ========================================================
            // FRAGMENT
            // ========================================================

            float4 frag(Varyings input)
                : SV_Target
            {
                // ----------------------------------------------------
                // Source
                // ----------------------------------------------------

                float4 source =
                    tex2D(
                        _MainTex,
                        input.uv
                    );

                float sourceAlpha =
                    source.a *
                    input.color.a;

                // ----------------------------------------------------
                // Blurred alpha
                // ----------------------------------------------------

                float blurredAlpha =
                    CalculateBlurredAlpha(
                        input.uv
                    );

                // ----------------------------------------------------
                // Outer glow
                // ----------------------------------------------------

                float glow =
                    saturate(
                        blurredAlpha -
                        source.a
                    );

                // ----------------------------------------------------
                // Falloff
                // ----------------------------------------------------

                glow =
                    pow(
                        glow,
                        _GlowFalloff
                    );

                // ----------------------------------------------------
                // Edge
                // ----------------------------------------------------

                float edge =
                    CalculateEdgeGlow(
                        source.a,
                        blurredAlpha
                    );

                glow +=
                    edge *
                    _EdgeIntensity;

                glow =
                    saturate(glow);

                // ----------------------------------------------------
                // Pulse
                // ----------------------------------------------------

                glow *=
                    CalculatePulse();

                // ----------------------------------------------------
                // Noise
                // ----------------------------------------------------

                glow *=
                    CalculateNoise(
                        input.uv
                    );

                // ----------------------------------------------------
                // Glow alpha
                // ----------------------------------------------------

                float glowAlpha =
                    glow *
                    _GlowColor.a *
                    _GlowIntensity;

                glowAlpha =
                    saturate(
                        glowAlpha
                    );

                // ----------------------------------------------------
                // Glow RGB
                // ----------------------------------------------------

                float3 glowRGB =
                    _GlowColor.rgb *
                    glowAlpha;

                // ----------------------------------------------------
                // Core
                // ----------------------------------------------------

                float3 coreRGB =
                    source.rgb *
                    input.color.rgb;

                // ----------------------------------------------------
                // Composite
                //
                // Glow находится ПОД самим UI.
                // ----------------------------------------------------

                float finalAlpha =
                    sourceAlpha +
                    glowAlpha *
                    (1.0 - sourceAlpha);

                float3 finalRGB =
                    coreRGB *
                    sourceAlpha +
                    glowRGB *
                    (1.0 - sourceAlpha);

                // ----------------------------------------------------
                // UI clipping
                // ----------------------------------------------------

                #ifdef UNITY_UI_CLIP_RECT

                    finalAlpha *=
                        UnityGet2DClipping(
                            input.worldPosition.xy,
                            _ClipRect
                        );

                #endif

                // ----------------------------------------------------
                // Alpha clip
                // ----------------------------------------------------

                #ifdef UNITY_UI_ALPHACLIP

                    clip(
                        finalAlpha -
                        0.001
                    );

                #endif

                return float4(
                    finalRGB,
                    finalAlpha
                );
            }

            ENDHLSL
        }
    }
}