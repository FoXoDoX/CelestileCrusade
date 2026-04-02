Shader "UI/CircleTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Smoothness ("Edge Smoothness", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            fixed4 _Color;
            float _Progress;
            float _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 diff = i.uv - center;

                float aspect = _ScreenParams.x / _ScreenParams.y;
                diff.x *= aspect;

                float dist = length(diff);
                float maxRadius = length(float2(aspect * 0.5, 0.5));
                float radius = (1.0 - _Progress) * maxRadius;

                // Чёрное СНАРУЖИ круга, прозрачное ВНУТРИ
                float alpha = smoothstep(radius - _Smoothness * maxRadius, radius, dist);

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}