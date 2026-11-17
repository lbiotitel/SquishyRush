Shader "UI/ShinningLogo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Shine Settings)]
        _ShineColor ("Shine Color", Color) = (1,1,1,1)        // 빛 색상 (HDR 지원 가능)
        _ShineSpeed ("Shine Speed", Float) = 2.0            // 빛이 스쳐지나가는 속도
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.15 // 빛줄기 두께
        _ShineInterval ("Shine Interval", Float) = 3.0      // 반짝임 주기 (초 단위)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ShineColor;
            float _ShineSpeed;
            float _ShineWidth;
            float _ShineInterval;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                float currentTime = fmod(_Time.y, _ShineInterval);
                
                float shine = 0.0;
               
                if (currentTime < 1.0) 
                {
                    float progress = currentTime * _ShineSpeed;
                    
                    float shinePosition = (IN.texcoord.x - IN.texcoord.y) + 0.5 - progress;
                    
                    shine = smoothstep(_ShineWidth, 0.0, abs(shinePosition));
                }
                
                fixed3 finalShine = shine * _ShineColor.rgb * color.a;
                
                color.rgb += finalShine;

                return color;
            }
            ENDCG
        }
    }
}