
Shader "XUIBasic"
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

        _CoordTransform( "CoordTransform", Vector ) = ( 1, 1, 0, 0 )
        _CoordCenterv2( "CoordCenter(v2)", Vector ) = ( 0.5, 0.5, 0, 0 )
        _CoordRotateAngle( "CoordRotateAngle", Float ) = 0
        [KeywordEnum( Gradient,Polygon )] _ElementType( "Element Type", Float ) = 0
        [KeywordEnum( Linear,Sin,Cos,Exponential )] _GradientType( "GradientType", Float ) = 0
        _ExpNumber( "ExpNumber", Float ) = 1
        _ColorA( "Color A", Color ) = ( 0, 1, 0.07612991, 1 )
        _ColorB( "Color B", Color ) = ( 1, 0, 0, 1 )
        _PolygonSides( "PolygonSides", Float ) = 5
        _PolygonSizev2( "PolygonSize(v2)", Vector ) = ( 0.5, 0.5, 0, 0 )
        [KeywordEnum( Normal,Mode,ABS )] _CoordRange( "CoordRange", Float ) = 0

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
            #define ASE_VERSION 19901

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define ASE_NEEDS_TEXTURE_COORDINATES0
            #define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
            #pragma shader_feature_local _ELEMENTTYPE_GRADIENT _ELEMENTTYPE_POLYGON
            #pragma shader_feature_local _GRADIENTTYPE_LINEAR _GRADIENTTYPE_SIN _GRADIENTTYPE_COS _GRADIENTTYPE_EXPONENTIAL
            #pragma shader_feature_local _COORDRANGE_NORMAL _COORDRANGE_MODE _COORDRANGE_ABS


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

            uniform float4 _ColorA;
            uniform float4 _ColorB;
            uniform float4 _CoordTransform;
            uniform float2 _CoordCenterv2;
            uniform float _CoordRotateAngle;
            uniform float _ExpNumber;
            uniform float2 _PolygonSizev2;
            uniform float _PolygonSides;


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

                float2 texCoord1 = IN.texcoord.xy * (_CoordTransform).xy + (_CoordTransform).zw;
                float cos2 = cos( ( ( _CoordRotateAngle * 2.0 ) * UNITY_PI ) );
                float sin2 = sin( ( ( _CoordRotateAngle * 2.0 ) * UNITY_PI ) );
                float2 rotator2 = mul( texCoord1 - _CoordCenterv2 , float2x2( cos2 , -sin2 , sin2 , cos2 )) + _CoordCenterv2;
                float2 temp_output_2_0_g3 = rotator2;
                float2 temp_output_3_0_g3 = float2( 1,1 );
                float2 temp_output_1_0_g3 = ( temp_output_2_0_g3 % temp_output_3_0_g3 );
                float2 temp_output_6_0_g3 = saturate( sign( temp_output_2_0_g3 ) );
                float2 lerpResult4_g3 = lerp( ( ( temp_output_1_0_g3 + temp_output_3_0_g3 ) % temp_output_3_0_g3 ) , temp_output_1_0_g3 , temp_output_6_0_g3);
                #if defined( _COORDRANGE_NORMAL )
                float2 staticSwitch26 = rotator2;
                #elif defined( _COORDRANGE_MODE )
                float2 staticSwitch26 = lerpResult4_g3;
                #elif defined( _COORDRANGE_ABS )
                float2 staticSwitch26 = abs( rotator2 );
                #else
                float2 staticSwitch26 = rotator2;
                #endif
                float temp_output_18_0 = (staticSwitch26).x;
                #if defined( _GRADIENTTYPE_LINEAR )
                float staticSwitch13 = temp_output_18_0;
                #elif defined( _GRADIENTTYPE_SIN )
                float staticSwitch13 = sin( temp_output_18_0 );
                #elif defined( _GRADIENTTYPE_COS )
                float staticSwitch13 = cos( temp_output_18_0 );
                #elif defined( _GRADIENTTYPE_EXPONENTIAL )
                float staticSwitch13 = pow( temp_output_18_0 , _ExpNumber );
                #else
                float staticSwitch13 = temp_output_18_0;
                #endif
                float temp_output_2_0_g4 = _PolygonSides;
                float cosSides12_g4 = cos( ( UNITY_PI / temp_output_2_0_g4 ) );
                float2 appendResult18_g4 = (float2(( _PolygonSizev2.x * cosSides12_g4 ) , ( _PolygonSizev2.y * cosSides12_g4 )));
                float2 break23_g4 = ( (staticSwitch26*2.0 + -1.0) / appendResult18_g4 );
                float polarCoords30_g4 = atan2( break23_g4.x , -break23_g4.y );
                float temp_output_52_0_g4 = ( 6.28318548202515 / temp_output_2_0_g4 );
                float2 appendResult25_g4 = (float2(break23_g4.x , -break23_g4.y));
                float2 finalUVs29_g4 = appendResult25_g4;
                float temp_output_44_0_g4 = ( cos( ( ( floor( ( 0.5 + ( polarCoords30_g4 / temp_output_52_0_g4 ) ) ) * temp_output_52_0_g4 ) - polarCoords30_g4 ) ) * length( finalUVs29_g4 ) );
                #if defined( _ELEMENTTYPE_GRADIENT )
                float staticSwitch11 = staticSwitch13;
                #elif defined( _ELEMENTTYPE_POLYGON )
                float staticSwitch11 = saturate( ( ( 1.0 - temp_output_44_0_g4 ) / fwidth( temp_output_44_0_g4 ) ) );
                #else
                float staticSwitch11 = staticSwitch13;
                #endif
                float4 lerpResult14 = lerp( _ColorA , _ColorB , staticSwitch11);
                float4 appendResult7 = (float4(lerpResult14));
                

                half4 color = appendResult7;

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