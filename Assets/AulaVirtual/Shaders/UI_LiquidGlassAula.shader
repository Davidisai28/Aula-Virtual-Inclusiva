Shader "AulaVirtual/UI/LiquidGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 uv : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 bottom : TEXCOORD2;
                float4 rim : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 bottom : TEXCOORD2;
                float4 rim : TEXCOORD3;
                float2 localPosition : TEXCOORD4;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float4 _ClipRect;
            int _UIVertexColorAlwaysGammaSpace;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv=v.uv.xy; o.shape=v.shape; o.bottom=v.bottom; o.rim=v.rim;
                o.localPosition=v.vertex.xy; o.color=v.color;
                // Canvas convierte COLOR a lineal; las paletas que enviamos por UV no
                // reciben esa conversión. Aplicarla evita el degradado inferior gris lavado.
                #ifndef UNITY_COLORSPACE_GAMMA
                if (_UIVertexColorAlwaysGammaSpace) o.color.rgb = GammaToLinearSpace(o.color.rgb);
                o.bottom.rgb = GammaToLinearSpace(o.bottom.rgb);
                o.rim.rgb = GammaToLinearSpace(o.rim.rgb);
                #endif
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 size = max(i.shape.xy, .001);
                float radius = min(i.shape.z, min(size.x,size.y)*.5);
                float2 q = abs((i.uv-.5)*size) - size*.5 + radius;
                float d = length(max(q,0)) + min(max(q.x,q.y),0) - radius;
                float aa = max(fwidth(d), .5);
                float silhouette = 1-smoothstep(-aa,aa,d);
                float edgeWidth = max(i.shape.w,aa*1.2);
                float edge = (1-smoothstep(edgeWidth,edgeWidth+aa,abs(d))) * i.rim.a;
                float fresnel = pow(saturate(1-abs(d)/max(radius*.6,10)),3);
                float sheen = exp2(-160*pow(i.uv.y-(.87-.18*i.uv.x),2));
                float reflection = exp2(-24*pow(i.uv.x-.16,2)-45*pow(i.uv.y-.8,2));
                float3 baseColor = lerp(i.bottom.rgb,i.color.rgb,smoothstep(0,1,i.uv.y));
                baseColor += float3(.014,.015,.016)*sheen + float3(.006,.007,.008)*reflection;
                baseColor += fresnel * float3(.004,.005,.005);
                baseColor = lerp(baseColor,i.rim.rgb,edge*.22);
                fixed4 result = fixed4(baseColor,lerp(i.bottom.a,i.color.a,i.uv.y)*silhouette);
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.localPosition,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a-.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
