// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Varneon/VUdon/MusicPlayer/UILoadingIcon"
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #define ASE_VERSION 19912

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

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

            float4 CalculateContrast( float contrastValue, float4 colorTarget )
            {
            	float t = 0.5 * ( 1.0 - contrastValue );
            	return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
            }


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

                float2 _Center1 = float2(0.5,0.5);
                float cos46 = cos( -4.0 * _Time.y );
                float sin46 = sin( -4.0 * _Time.y );
                float2 rotator46 = mul( IN.texcoord.xy - _Center1 , float2x2( cos46 , -sin46 , sin46 , cos46 )) + _Center1;
                float2 break30 = ( rotator46 - _Center1 );
                float temp_output_69_0 = ( _Time.y % 2.0 );
                float ifLocalVar55 = 0;
                if( ( _Time.y % 4.0 ) <= 2.0 )
                ifLocalVar55 = 1.0;
                float lerpResult74 = lerp( temp_output_69_0 , -temp_output_69_0 , ifLocalVar55);
                float4 temp_cast_0 = ( (lerpResult74 + ( atan2( break30.x , break30.y ) - UNITY_PI ) * ( 0.0 - lerpResult74 ) / ( -( ( 1.0 - temp_output_69_0 ) * UNITY_PI ) - UNITY_PI ) )).xxxx;
                float clampResult16 = clamp( distance( IN.texcoord.xy , float2( 0.5,0.5 ) ) , 0.25 , 1.0 );
                float clampResult47 = clamp(  (0.0 + ( cos( ( clampResult16 * ( 5.0 * UNITY_PI ) ) ) - 0.25 ) * ( 1.0 - 0.0 ) / ( 0.5 - 0.25 ) ) , 0.0 , 1.0 );
                float clampResult42 = clamp( CalculateContrast(1000.0,temp_cast_0).r , 0.0 , clampResult47 );
                float4 temp_cast_1 = (clampResult42).xxxx;
                

                half4 color = temp_cast_1;

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
Version=19912
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":29,"pos":[-1614.109,-21.2326],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":33,"pos":[-1593.809,97.26741],"params":["Inherit","False","Constant","_Center1","Center","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5,0.5","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":7,"pos":[-1589.532,360.2937],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor","id":46,"pos":[-1393.981,-19.88665],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","-4","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor","id":69,"pos":[-1382.55,272.2524],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","2","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":6,"pos":[-1424,616],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1,"pos":[-1408,736],"params":["Inherit","False","Constant","_Center","Center","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5,0.5","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":32,"pos":[-1194.209,-18.03246],"params":["Inherit","False","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":78,"pos":[-1190.02,265.4058],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":56,"pos":[-1361.509,529.4565],"params":["Inherit","False","Constant","_TRUE","TRUE","0","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor","id":53,"pos":[-1381.009,426.1565],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","4","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor","id":3,"pos":[-1168,616],"params":["Inherit","False","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":30,"pos":[-1045.609,-19.03252],"params":["Inherit","False","FLOAT2","1","0","FLOAT2","0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.PiNode, AmplifyShaderEditor","id":44,"pos":[-1038.506,264.9781],"params":["Inherit","False","1","0","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ConditionalIfNode, AmplifyShaderEditor","id":55,"pos":[-1185.91,438.0567],"params":["Inherit","False","False","5","0","FLOAT","0","False","1","FLOAT","2","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NegateNode, AmplifyShaderEditor","id":75,"pos":[-1176.422,355.8055],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor","id":16,"pos":[-1000,616],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.25","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PiNode, AmplifyShaderEditor","id":15,"pos":[-1032,736],"params":["Inherit","False","1","0","FLOAT","5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ATan2OpNode, AmplifyShaderEditor","id":31,"pos":[-923.61,-19.03252],"params":["Inherit","True","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PiNode, AmplifyShaderEditor","id":50,"pos":[-906.2444,193.4945],"params":["Inherit","False","1","0","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NegateNode, AmplifyShaderEditor","id":45,"pos":[-856.6078,265.678],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":74,"pos":[-885.1506,342.152],"params":["Inherit","False","3","0","FLOAT","-1","False","1","FLOAT","1","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":12,"pos":[-816,616],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","10","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":43,"pos":[-656,-16],"params":["Inherit","True","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","1","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CosOpNode, AmplifyShaderEditor","id":14,"pos":[-672,616],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleContrastOpNode, AmplifyShaderEditor","id":51,"pos":[-376,-16],"params":["Inherit","True","2","1","COLOR","0,0,0,0","False","0","FLOAT","1000","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":26,"pos":[-520,616],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0.25","False","2","FLOAT","0.5","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":52,"pos":[-136,-16],"params":["Inherit","False","COLOR","1","0","COLOR","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor","id":47,"pos":[-312,616],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor","id":42,"pos":[-8,-16],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":0,"pos":[144,-16],"params":["Float","False","True","-1","2","AmplifyShaderEditor.MaterialInspector","0","12","Varneon/VUdon/MusicPlayer/UILoadingIcon","5056123faa0c79b47ab6ad7e8bf059a4","True","Default","0","0","Default","2","False","True","2","5","False","","10","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","True","True","True","True","True","0","True","_ColorMask","False","False","False","False","False","False","False","True","True","0","True","_Stencil","255","True","_StencilReadMask","255","True","_StencilWriteMask","0","True","_StencilComp","0","True","_StencilOp","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","2","False","","True","0","True","unity_GUIZTestMode","False","False","True","5","Queue=Transparent=Queue=0","IgnoreProjector=True","RenderType=Transparent=RenderType","PreviewType=Plane","CanUseSpriteAtlas=True","False","False","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","0","","0","0","Standard","0","0","1","True","False","","False","0"]}
{"wire":[46,0,29,0]}
{"wire":[46,1,33,0]}
{"wire":[69,0,7,0]}
{"wire":[32,0,46,0]}
{"wire":[32,1,33,0]}
{"wire":[78,0,69,0]}
{"wire":[53,0,7,0]}
{"wire":[3,0,6,0]}
{"wire":[3,1,1,0]}
{"wire":[30,0,32,0]}
{"wire":[44,0,78,0]}
{"wire":[55,0,53,0]}
{"wire":[55,3,56,0]}
{"wire":[55,4,56,0]}
{"wire":[75,0,69,0]}
{"wire":[16,0,3,0]}
{"wire":[31,0,30,0]}
{"wire":[31,1,30,1]}
{"wire":[45,0,44,0]}
{"wire":[74,0,69,0]}
{"wire":[74,1,75,0]}
{"wire":[74,2,55,0]}
{"wire":[12,0,16,0]}
{"wire":[12,1,15,0]}
{"wire":[43,0,31,0]}
{"wire":[43,1,50,0]}
{"wire":[43,2,45,0]}
{"wire":[43,3,74,0]}
{"wire":[14,0,12,0]}
{"wire":[51,1,43,0]}
{"wire":[26,0,14,0]}
{"wire":[52,0,51,0]}
{"wire":[47,0,26,0]}
{"wire":[42,0,52,0]}
{"wire":[42,2,47,0]}
{"wire":[0,0,42,0]}
ASEEND*/
//CHKSM=AD2EC6300A8890A4FAE15F2C267873834FBD027E