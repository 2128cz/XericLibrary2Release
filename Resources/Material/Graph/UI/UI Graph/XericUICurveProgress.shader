Shader "XericLibrary/UIGraph/XericUICurveProgress"
{
    Properties
    {
        [PerRendererData] _MainTex ("Progress Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Tile ("Tile Count Along Path", Float) = 1
        _Scroll ("Scroll Speed (along +progress)", Float) = 0
        _ScrollPhase ("Scroll Phase", Range(0, 1)) = 0

        _Cutoff ("Progress Cutoff", Range(0, 1)) = 1
        _CutoffSoftness ("Cutoff Softness", Range(0, 0.5)) = 0.02

        _EdgeExp ("Edge Exp", Range(0.001, 1)) = 0.8671591
        _EdgeStep ("Edge Step", Range(0, 1)) = 0
        _EdgeStepRange ("Edge Step Range", Range(0, 0.5)) = 0.1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
            Comp [_StencilComp]
            Pass [_StencilOp]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv0      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask     : TEXCOORD2;
                float featherOn : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float _Tile;
            float _Scroll;
            float _ScrollPhase;
            float _Cutoff;
            float _CutoffSoftness;
            float _EdgeExp;
            float _EdgeStep;
            float _EdgeStepRange;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.uv0 = v.uv0;
                OUT.featherOn = v.uv1.x;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 与 UI 默认着色器同样的处理：把插值出来的 alpha 对齐到 1/256，
                // 否则预乘混合下容易抖
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0 / alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;

                // uv0.y 已经是"沿整条面片的进度"，横向不重复，纵向按 _Tile 平铺并可随时间流动
                float2 sampleUV = float2(
                    IN.uv0.x,
                    IN.uv0.y * _Tile - _Time.y * _Scroll + _ScrollPhase);
                fixed4 tex = tex2D(_MainTex, sampleUV);

                float edge = smoothstep(
                    _EdgeStep - _EdgeStepRange * 2.0,
                    _EdgeStep + _EdgeStepRange,
                    sin(IN.uv0.x * UNITY_PI));
                float4 color = lerp(IN.color, IN.color * pow(saturate(edge), _EdgeExp), IN.featherOn);
                color *= tex;
                float soft = max(_CutoffSoftness, 1e-4);
                color.a *= smoothstep(_Cutoff + soft, _Cutoff - soft, IN.uv0.y);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
