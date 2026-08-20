Shader "FX/WinLine"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (1, 0.92, 0.016, 1)
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _LineWidth ("Line Width Multiplier", Float) = 1.0
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex       : POSITION;
                float2 texcoord     : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                float2 texcoord     : TEXCOORD0;
                float4 color        : COLOR;
                float depth         : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _GlowColor;
            fixed4 _CoreColor;
            float _LineWidth;
            float _GlowIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                OUT.depth = OUT.vertex.z / OUT.vertex.w;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // TEXCOORD0.x содержит расстояние от центра линии (0 = центр, 0.5 = край)
                float distFromCenter = abs(IN.texcoord.x - 0.5) * 2.0;

                // Базовый альфа-канал для основной линии
                float baseAlpha = 1.0 - smoothstep(0.7, 1.0, distFromCenter);

                // Градиент от ядра к свечению
                float coreFactor = 1.0 - smoothstep(0.0, 0.4, distFromCenter);
                float glowFactor = smoothstep(0.3, 1.0, distFromCenter);

                // Смешиваем цвета: ядро в центре, свечение по краям
                fixed3 lineColor = lerp(_GlowColor.rgb, _CoreColor.rgb, coreFactor);

                // Интенсивность свечения
                float glowAlpha = _GlowColor.a * glowFactor * _GlowIntensity;
                float coreAlpha = _CoreColor.a * coreFactor;

                float finalAlpha = max(glowAlpha, coreAlpha * baseAlpha);

                // Применяем цвет вершины (если есть)
                lineColor *= IN.color.rgb;
                finalAlpha *= IN.color.a;

                return fixed4(lineColor, finalAlpha);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}