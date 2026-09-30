// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Varneon/VUdon/MusicPlayer/UIGlassBlurredNoise"
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

            float3 HSVToRGB( float3 c )
            {
            	float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
            	float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
            	return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
            }
            
            float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
            float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
            float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
            float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
            float snoise( float3 v )
            {
            	const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
            	float3 i = floor( v + dot( v, C.yyy ) );
            	float3 x0 = v - i + dot( i, C.xxx );
            	float3 g = step( x0.yzx, x0.xyz );
            	float3 l = 1.0 - g;
            	float3 i1 = min( g.xyz, l.zxy );
            	float3 i2 = max( g.xyz, l.zxy );
            	float3 x1 = x0 - i1 + C.xxx;
            	float3 x2 = x0 - i2 + C.yyy;
            	float3 x3 = x0 - 0.5;
            	i = mod3D289( i);
            	float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
            	float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
            	float4 x_ = floor( j / 7.0 );
            	float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
            	float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
            	float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
            	float4 h = 1.0 - abs( x ) - abs( y );
            	float4 b0 = float4( x.xy, y.xy );
            	float4 b1 = float4( x.zw, y.zw );
            	float4 s0 = floor( b0 ) * 2.0 + 1.0;
            	float4 s1 = floor( b1 ) * 2.0 + 1.0;
            	float4 sh = -step( h, 0.0 );
            	float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
            	float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
            	float3 g0 = float3( a0.xy, h.x );
            	float3 g1 = float3( a0.zw, h.y );
            	float3 g2 = float3( a1.xy, h.z );
            	float3 g3 = float3( a1.zw, h.w );
            	float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
            	g0 *= norm.x;
            	g1 *= norm.y;
            	g2 *= norm.z;
            	g3 *= norm.w;
            	float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
            	m = m* m;
            	m = m* m;
            	float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
            	return 42.0 * dot( m, px);
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

                float3 linearToGamma48 = LinearToGammaSpace( IN.color.rgb );
                float3 break47 = linearToGamma48;
                float3 hsvTorgb43 = HSVToRGB( float3(break47.x,break47.z,IN.color.a) );
                float mulTime39 = _Time.y * 0.25;
                float3 appendResult38 = (float3(IN.texcoord.xy , mulTime39));
                float simplePerlin3D37 = snoise( appendResult38 );
                simplePerlin3D37 = simplePerlin3D37*0.5 + 0.5;
                float3 hsvTorgb45 = HSVToRGB( float3(break47.y,break47.z,IN.color.a) );
                float3 appendResult40 = (float3(IN.texcoord.xy.y , IN.texcoord.xy.x , -mulTime39));
                float simplePerlin3D42 = snoise( appendResult40*0.75 );
                simplePerlin3D42 = simplePerlin3D42*0.5 + 0.5;
                float3 blendOpSrc31 = ( hsvTorgb43 * simplePerlin3D37 );
                float3 blendOpDest31 = ( hsvTorgb45 * simplePerlin3D42 );
                float3 break33 = ( saturate( max( blendOpSrc31, blendOpDest31 ) ));
                float4 appendResult34 = (float4(break33.x , break33.y , break33.z , 1.0));
                

                half4 color = appendResult34;

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
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":44,"pos":[-1043.883,-312.2073],"params":["Inherit","True","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":36,"pos":[-835.875,-9.281385],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.NegateNode, AmplifyShaderEditor","id":41,"pos":[-773.8408,119.9298],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":39,"pos":[-950.4286,120.1163],"params":["Inherit","False","1","0","FLOAT","0.25","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.LinearToGammaNode, AmplifyShaderEditor","id":48,"pos":[-787.3546,-312.3664],"params":["Inherit","False","0","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":40,"pos":[-585.8057,119.3449],"params":["Inherit","False","FLOAT3","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":38,"pos":[-586.5125,-9.853628],"params":["Inherit","False","FLOAT3","4","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":47,"pos":[-580.0546,-312.2695],"params":["Inherit","False","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":37,"pos":[-415.6974,-14.86514],"params":["Inherit","False","Simplex3D","True","False","2","0","FLOAT3","0,0,0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":42,"pos":[-419.821,114.5931],"params":["Inherit","False","Simplex3D","True","False","2","0","FLOAT3","0,0,0","False","1","FLOAT","0.75","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.HSVToRGBNode, AmplifyShaderEditor","id":45,"pos":[-439.5071,-164.8154],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.75","False","2","FLOAT","0.5","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.HSVToRGBNode, AmplifyShaderEditor","id":43,"pos":[-440.0637,-311.3539],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.75","False","2","FLOAT","0.5","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":28,"pos":[-147.6224,94.03571],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":24,"pos":[-146.2088,-33.70678],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.BlendOpsNode, AmplifyShaderEditor","id":31,"pos":[22.98938,66.13097],"params":["Inherit","False","Lighten","True","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","1","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":33,"pos":[236.545,71.71274],"params":["Inherit","False","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":34,"pos":[382.2448,71.71277],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","1","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":0,"pos":[532.6998,71.79999],"params":["Float","False","True","-1","2","AmplifyShaderEditor.MaterialInspector","0","12","Varneon/VUdon/MusicPlayer/UIGlassBlurredNoise","5056123faa0c79b47ab6ad7e8bf059a4","True","Default","0","0","Default","2","False","True","2","5","False","","10","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","True","True","True","True","True","0","True","_ColorMask","False","False","False","False","False","False","False","True","True","0","True","_Stencil","255","True","_StencilReadMask","255","True","_StencilWriteMask","0","True","_StencilComp","0","True","_StencilOp","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","2","False","","True","0","True","unity_GUIZTestMode","False","False","True","5","Queue=Transparent=Queue=0","IgnoreProjector=True","RenderType=Transparent=RenderType","PreviewType=Plane","CanUseSpriteAtlas=True","False","False","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","0","","0","0","Standard","0","0","1","True","False","","False","0"]}
{"wire":[41,0,39,0]}
{"wire":[48,0,44,0]}
{"wire":[40,0,36,2]}
{"wire":[40,1,36,1]}
{"wire":[40,2,41,0]}
{"wire":[38,0,36,0]}
{"wire":[38,2,39,0]}
{"wire":[47,0,48,0]}
{"wire":[37,0,38,0]}
{"wire":[42,0,40,0]}
{"wire":[45,0,47,1]}
{"wire":[45,1,47,2]}
{"wire":[45,2,44,4]}
{"wire":[43,0,47,0]}
{"wire":[43,1,47,2]}
{"wire":[43,2,44,4]}
{"wire":[28,0,45,0]}
{"wire":[28,1,42,0]}
{"wire":[24,0,43,0]}
{"wire":[24,1,37,0]}
{"wire":[31,0,24,0]}
{"wire":[31,1,28,0]}
{"wire":[33,0,31,0]}
{"wire":[34,0,33,0]}
{"wire":[34,1,33,1]}
{"wire":[34,2,33,2]}
{"wire":[0,0,34,0]}
ASEEND*/
//CHKSM=E8F34101A44652C1BDF04FC2D1F085CA767CEE3A