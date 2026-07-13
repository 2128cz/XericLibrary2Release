
Shader "XericLibrary/BluePrint/BlueprintBackgound_GridLine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _Transform( "Transform", Vector ) = ( 10, 10, 0, 0 )
        _GridOverlayPower( "Grid Overlay Power", Float ) = 5
        _GridColor( "Grid Color", Color ) = ( 1, 1, 1, 1 )
        _GridLineThreshold( "Grid Line Threshold ", Range( 0, 1 ) ) = 0.9888518
        _GridBackgroundColor( "Grid Background Color", Color ) = ( 0, 0, 0, 1 )
        _GridExp( "Grid Exp", Range( 0.001, 1 ) ) = 0.001
        _LodTransthd( "Lod Trans thd", Float ) = 30

    }

    SubShader
    {
		LOD 0

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
            #define ASE_VERSION 19908

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityShaderVariables.cginc"
            #define ASE_NEEDS_TEXTURE_COORDINATES0
            #define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
                
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            uniform float4 _GridBackgroundColor;
            uniform float4 _GridColor;
            uniform float _GridLineThreshold;
            uniform float4 _Transform;
            uniform float _LodTransthd;
            uniform float _GridOverlayPower;
            uniform float _GridExp;


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                

                v.vertex.xyz +=  float3( 0, 0, 0 ) ;

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = v.texcoord;
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN ) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float4 temp_cast_0 = (_GridLineThreshold).xxxx;
                float2 texCoord2 = IN.texcoord.xy * (_Transform).xy + (_Transform).zw;
                float2 temp_output_62_0 = fwidth( texCoord2 );
                float4 appendResult73 = (float4(temp_output_62_0 , temp_output_62_0));
                float4 temp_cast_1 = (1.0).xxxx;
                float2 temp_cast_2 = (0.5).xx;
                float smoothstepResult95 = smoothstep( -0.8 , 1.0 , ( ( max( _Transform.x, _Transform.y ) - _LodTransthd ) / _LodTransthd ));
                float2 lerpResult94 = lerp( frac( texCoord2 ) , temp_cast_2 , smoothstepResult95);
                float4 appendResult5 = (float4(lerpResult94 , frac( ( texCoord2 / _GridOverlayPower ) )));
                float temp_output_85_0 = ( 0.5 * 1.1 );
                float4 appendResult83 = (float4(0.5 , 0.5 , temp_output_85_0 , temp_output_85_0));
                float4 smoothstepResult33 = smoothstep( ( _ScreenParams.z * ( temp_cast_0 - appendResult73 ) ) , temp_cast_1 , ( abs( ( appendResult5 + -0.5 ) ) / appendResult83 ));
                float4 break35 = smoothstepResult33;
                float4 lerpResult46 = lerp( _GridBackgroundColor , _GridColor , saturate( pow( ( break35.x + break35.y + break35.z + break35.w ) , _GridExp ) ));
                

                half4 color = lerpResult46;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
    CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}