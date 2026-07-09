
Shader "XericLibrary/UIGraph/UIPattern"
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

        _BorderColorAlpha( "BorderColorAlpha", Range( -0.1, 1 ) ) = 1
        _BorderThinkness( "Border Thinkness", Range( 0.001, 1 ) ) = 1
        _BorderExp( "Border Exp", Range( 0.001, 1 ) ) = 0.001
        _BackGroundColorAlpha( "BackGroundColorAlpha", Range( -0.1, 1 ) ) = 0
        _ContentThinkness( "Content Thinkness", Range( 0.001, 1 ) ) = 1
        _ContentExp( "Content Exp", Range( 0.001, 1 ) ) = 0.001

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
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define ASE_NEEDS_FRAG_NORMAL
            #define ASE_NEEDS_FRAG_COLOR
            #define ASE_NEEDS_TEXTURE_COORDINATES2
            #define ASE_NEEDS_FRAG_TANGENT


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float3 ase_normal : NORMAL;
                float4 ase_texcoord2 : TEXCOORD2;
                float4 ase_tangent : TANGENT;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
                float3 ase_normal : NORMAL;
                float4 ase_texcoord3 : TEXCOORD3;
                float4 ase_tangent : TANGENT;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            uniform float _BackGroundColorAlpha;
            uniform float _ContentThinkness;
            uniform float _ContentExp;
            uniform float _BorderColorAlpha;
            uniform float _BorderThinkness;
            uniform float _BorderExp;


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.ase_normal = v.ase_normal;
                OUT.ase_texcoord3.xy = v.ase_texcoord2.xy;
                OUT.ase_tangent = v.ase_tangent;
                
                //setting value to unused interpolator channels and avoid initialization warnings
                OUT.ase_texcoord3.zw = 0;

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

                float4 appendResult60 = (float4(IN.ase_normal , saturate( max( max( IN.ase_normal.x, IN.ase_normal.y ), IN.ase_normal.z ) )));
                float4 appendResult43 = (float4(IN.ase_normal , _BackGroundColorAlpha));
                float4 lerpResult50 = lerp( appendResult60 , appendResult43 , saturate( sign( ( _BackGroundColorAlpha + 0.001 ) ) ));
                float4 Background_Color39 = lerpResult50;
                float4 Center_Color40 = IN.color;
                float2 texCoord4 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
                float lerpResult107 = lerp( texCoord4.x , 0.0 , texCoord4.y);
                float4 lerpResult64 = lerp( Background_Color39 , Center_Color40 , pow( ( ( _ContentThinkness - lerpResult107 ) / _ContentThinkness ) , _ContentExp ));
                float4 appendResult57 = (float4(IN.ase_tangent.xyz , saturate( max( max( IN.ase_tangent.xyz.x, IN.ase_tangent.xyz.y ), IN.ase_tangent.xyz.z ) )));
                float4 appendResult44 = (float4(IN.ase_tangent.xyz , _BorderColorAlpha));
                float4 lerpResult51 = lerp( appendResult57 , appendResult44 , saturate( sign( ( _BorderColorAlpha + 0.001 ) ) ));
                float4 Border_Color41 = lerpResult51;
                float lerpResult108 = lerp( 0.0 , texCoord4.x , texCoord4.y);
                float4 lerpResult82 = lerp( Background_Color39 , Border_Color41 , pow( ( ( _BorderThinkness - lerpResult108 ) / _BorderThinkness ) , _BorderExp ));
                float4 lerpResult109 = lerp( lerpResult64 , lerpResult82 , texCoord4.y);
                

                half4 color = lerpResult109;

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