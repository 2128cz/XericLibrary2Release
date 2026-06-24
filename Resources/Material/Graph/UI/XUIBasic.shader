// Made with Amplify Shader Editor v1.9.9.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
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
/*ASEBEGIN
Version=19901
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;5;-1408,256;Inherit;False;Property;_CoordRotateAngle;CoordRotateAngle;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;28;-1920,0;Inherit;False;Property;_CoordTransform;CoordTransform;0;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;-1184,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;30;-1664,96;Inherit;False;False;False;True;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;29;-1664,16;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;1;-1408,0;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PiNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;8;-1024,256;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;4;-1408,128;Inherit;False;Property;_CoordCenterv2;CoordCenter(v2);1;0;Create;True;0;0;0;False;0;False;0.5,0.5;0.5,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-768,-128;Inherit;False;Constant;_Vector0;Vector 0;11;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;2;-896,0;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;32;-512,0;Inherit;False;PMod;-1;;3;f21979a9080cb044ca372458f5a37d88;1,33,0;2;2;FLOAT2;0,0;False;3;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-480,128;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;-256,-128;Inherit;False;Property;_CoordRange;CoordRange;10;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;3;Normal;Mode;ABS;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;18;0,-128;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;23;0,96;Inherit;False;Property;_ExpNumber;ExpNumber;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;19;256,-64;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;20;256,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;22;256,64;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;0,384;Inherit;False;Property;_PolygonSizev2;PolygonSize(v2);9;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;24;0,288;Inherit;False;Property;_PolygonSides;PolygonSides;8;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;256,256;Inherit;False;Polygon;-1;;4;6906ef7087298c94c853d6753e182169;0;4;1;FLOAT2;0,0;False;2;FLOAT;5;False;3;FLOAT;0.5;False;4;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;512,-128;Inherit;False;Property;_GradientType;GradientType;4;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;4;Linear;Sin;Cos;Exponential;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;896,-128;Inherit;False;Property;_ElementType;Element Type;3;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;2;Gradient;Polygon;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;896,-512;Inherit;False;Property;_ColorA;Color A;6;0;Create;True;0;0;0;False;0;False;0,1,0.07612991,1;0,1,0.07612991,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;16;896,-320;Inherit;False;Property;_ColorB;Color B;7;0;Create;True;0;0;0;False;0;False;1,0,0,1;1,0,0,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;1152,-384;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;7;1312,-384;Inherit;False;FLOAT4;4;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;0;1472,-384;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;XUIBasic;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;9;0;5;0
WireConnection;30;0;28;0
WireConnection;29;0;28;0
WireConnection;1;0;29;0
WireConnection;1;1;30;0
WireConnection;8;0;9;0
WireConnection;2;0;1;0
WireConnection;2;1;4;0
WireConnection;2;2;8;0
WireConnection;32;2;2;0
WireConnection;32;3;34;0
WireConnection;36;0;2;0
WireConnection;26;1;2;0
WireConnection;26;0;32;0
WireConnection;26;2;36;0
WireConnection;18;0;26;0
WireConnection;19;0;18;0
WireConnection;20;0;18;0
WireConnection;22;0;18;0
WireConnection;22;1;23;0
WireConnection;10;1;26;0
WireConnection;10;2;24;0
WireConnection;10;3;25;1
WireConnection;10;4;25;2
WireConnection;13;1;18;0
WireConnection;13;0;19;0
WireConnection;13;2;20;0
WireConnection;13;3;22;0
WireConnection;11;1;13;0
WireConnection;11;0;10;0
WireConnection;14;0;15;0
WireConnection;14;1;16;0
WireConnection;14;2;11;0
WireConnection;7;0;14;0
WireConnection;0;0;7;0
ASEEND*/
//CHKSM=EF1BCF878CBE9DBF87503B5564BA985D4602AE12