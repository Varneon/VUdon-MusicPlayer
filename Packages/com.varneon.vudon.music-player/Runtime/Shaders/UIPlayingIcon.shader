// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Varneon/VUdon/Music Player/UIPlayingIcon"
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #define ASE_VERSION 19912

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityShaderVariables.cginc"
            #define ASE_NEEDS_FRAG_COLOR
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

            float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
            float snoise( float2 v )
            {
            	const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
            	float2 i = floor( v + dot( v, C.yy ) );
            	float2 x0 = v - i + dot( i, C.xx );
            	float2 i1;
            	i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
            	float4 x12 = x0.xyxy + C.xxzz;
            	x12.xy -= i1;
            	i = mod2D289( i );
            	float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
            	float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
            	m = m * m;
            	m = m * m;
            	float3 x = 2.0 * frac( p * C.www ) - 1.0;
            	float3 h = abs( x ) - 0.5;
            	float3 ox = floor( x + 0.5 );
            	float3 a0 = x - ox;
            	m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
            	float3 g;
            	g.x = a0.x * x0.x + h.x * x0.y;
            	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            	return 130.0 * dot( m, g );
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

                float2 temp_output_20_0 = ( ( IN.texcoord.xy * float2( 3,3 ) ) + float2( 0,-1 ) );
                float temp_output_2_0_g1 = 0.5;
                float mulTime12 = _Time.y * 2.0;
                float2 appendResult9 = (float2(floor( ( IN.texcoord.xy.x * 3.0 ) ) , mulTime12));
                float simplePerlin2D8 = snoise( appendResult9*0.5 );
                simplePerlin2D8 = simplePerlin2D8*0.5 + 0.5;
                float temp_output_28_0 =  (1.0 + ( simplePerlin2D8 - 0.0 ) * ( 3.0 - 1.0 ) / ( 1.0 - 0.0 ) );
                float temp_output_3_0_g1 = temp_output_28_0;
                float2 appendResult21_g1 = (float2(temp_output_2_0_g1 , temp_output_3_0_g1));
                float Radius25_g1 = max( min( min( abs( ( 0.5 * 2 ) ), abs( temp_output_2_0_g1 ) ), abs( temp_output_3_0_g1 ) ), 1E-05 );
                float temp_output_30_0_g1 = ( length( max( ( ( abs( (temp_output_20_0*2.0 + -1.0) ) - appendResult21_g1 ) + Radius25_g1 ), 0.0 ) ) / Radius25_g1 );
                float2 temp_output_18_0 = ( temp_output_20_0 - float2( 1,0 ) );
                float temp_output_2_0_g2 = 0.5;
                float temp_output_3_0_g2 = temp_output_28_0;
                float2 appendResult21_g2 = (float2(temp_output_2_0_g2 , temp_output_3_0_g2));
                float Radius25_g2 = max( min( min( abs( ( 0.5 * 2 ) ), abs( temp_output_2_0_g2 ) ), abs( temp_output_3_0_g2 ) ), 1E-05 );
                float temp_output_30_0_g2 = ( length( max( ( ( abs( (temp_output_18_0*2.0 + -1.0) ) - appendResult21_g2 ) + Radius25_g2 ), 0.0 ) ) / Radius25_g2 );
                float temp_output_5_0 = saturate( ( ( 1.0 - temp_output_30_0_g2 ) / fwidth( temp_output_30_0_g2 ) ) );
                float temp_output_2_0_g3 = 0.5;
                float temp_output_3_0_g3 = temp_output_28_0;
                float2 appendResult21_g3 = (float2(temp_output_2_0_g3 , temp_output_3_0_g3));
                float Radius25_g3 = max( min( min( abs( ( 0.5 * 2 ) ), abs( temp_output_2_0_g3 ) ), abs( temp_output_3_0_g3 ) ), 1E-05 );
                float temp_output_30_0_g3 = ( length( max( ( ( abs( (( temp_output_18_0 - float2( 1,0 ) )*2.0 + -1.0) ) - appendResult21_g3 ) + Radius25_g3 ), 0.0 ) ) / Radius25_g3 );
                

                half4 color = ( IN.color * max( max( saturate( ( ( 1.0 - temp_output_30_0_g1 ) / fwidth( temp_output_30_0_g1 ) ) ), temp_output_5_0 ), max( temp_output_5_0, saturate( ( ( 1.0 - temp_output_30_0_g3 ) / fwidth( temp_output_30_0_g3 ) ) ) ) ) );

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
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":15,"pos":[-1624,88],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","3","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":12,"pos":[-1568,200],"params":["Inherit","False","1","0","FLOAT","2","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor","id":16,"pos":[-1472,112],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":2,"pos":[-1328,-136],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":9,"pos":[-1336,176],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":3,"pos":[-1072,-136],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","3,3","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":8,"pos":[-1168,176],"params":["Inherit","True","Simplex2D","True","False","2","0","FLOAT2","0,0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":20,"pos":[-856,-136],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,-1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":18,"pos":[-888,-32],"params":["Inherit","False","2","0","FLOAT2","0,0","False","1","FLOAT2","1,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":19,"pos":[-888,72],"params":["Inherit","False","2","0","FLOAT2","0,0","False","1","FLOAT2","1,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":28,"pos":[-920,176],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","1","False","4","FLOAT","3","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":4,"pos":[-888,-296],"params":["Inherit","False","Constant","_Width","Width","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":29,"pos":[-888,-216],"params":["Inherit","False","Constant","_Radius","Radius","0","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1,"pos":[-624,-136],"params":["Inherit","True","Rounded Rectangle","-1","","1","8679f72f5be758f47babb3ba1d5f51d3","0","4","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":5,"pos":[-624,96],"params":["Inherit","True","Rounded Rectangle","-1","","2","8679f72f5be758f47babb3ba1d5f51d3","0","4","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":6,"pos":[-624,320],"params":["Inherit","True","Rounded Rectangle","-1","","3","8679f72f5be758f47babb3ba1d5f51d3","0","4","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT","0","False","4","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":23,"pos":[-344,144],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":22,"pos":[-344,32],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor","id":24,"pos":[-216,72],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":26,"pos":[-336,-152],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":27,"pos":[-80,48],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":0,"pos":[88,48],"params":["Float","False","True","-1","3","AmplifyShaderEditor.MaterialInspector","0","12","Varneon/VUdon/Music Player/UIPlayingIcon","5056123faa0c79b47ab6ad7e8bf059a4","True","Default","0","0","Default","2","False","True","3","1","False","","10","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","True","True","True","True","True","0","True","_ColorMask","False","False","False","False","False","False","False","True","True","0","True","_Stencil","255","True","_StencilReadMask","255","True","_StencilWriteMask","0","True","_StencilComp","0","True","_StencilOp","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","2","False","","True","0","True","unity_GUIZTestMode","False","False","True","5","Queue=Transparent=Queue=0","IgnoreProjector=True","RenderType=Transparent=RenderType","PreviewType=Plane","CanUseSpriteAtlas=True","False","False","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","3","False","0","","0","0","Standard","0","0","1","True","False","","False","0"]}
{"wire":[15,0,2,1]}
{"wire":[16,0,15,0]}
{"wire":[9,0,16,0]}
{"wire":[9,1,12,0]}
{"wire":[3,0,2,0]}
{"wire":[8,0,9,0]}
{"wire":[20,0,3,0]}
{"wire":[18,0,20,0]}
{"wire":[19,0,18,0]}
{"wire":[28,0,8,0]}
{"wire":[1,1,20,0]}
{"wire":[1,2,4,0]}
{"wire":[1,3,28,0]}
{"wire":[1,4,29,0]}
{"wire":[5,1,18,0]}
{"wire":[5,2,4,0]}
{"wire":[5,3,28,0]}
{"wire":[5,4,29,0]}
{"wire":[6,1,19,0]}
{"wire":[6,2,4,0]}
{"wire":[6,3,28,0]}
{"wire":[6,4,29,0]}
{"wire":[23,0,5,0]}
{"wire":[23,1,6,0]}
{"wire":[22,0,1,0]}
{"wire":[22,1,5,0]}
{"wire":[24,0,22,0]}
{"wire":[24,1,23,0]}
{"wire":[27,0,26,0]}
{"wire":[27,1,24,0]}
{"wire":[0,0,27,0]}
ASEEND*/
//CHKSM=9919D221F0C59D0C7E31B8D1DB44C42083CDE242