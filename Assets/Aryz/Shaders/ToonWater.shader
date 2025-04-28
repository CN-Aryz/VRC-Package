Shader "Custom/ToonWater"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _Color ("Water Color", Color) = (0.2, 0.5, 1, 0.6)
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.1
        _WaveSpeed ("Wave Speed", Range(0.1, 5)) = 1.0
        _UVSpeed ("UV Scroll Speed", Vector) = (0.05, 0.02, 0, 0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _WaveStrength;
            float _WaveSpeed;
            float4 _UVSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // 顶点动画 - 水面波动（上下浮动）
                float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * 3.0 + v.vertex.z * 3.0);
                v.vertex.y += wave * _WaveStrength;

                o.vertex = UnityObjectToClipPos(v.vertex);

                // UV 动画 - 贴图滚动
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv += _Time.y * _UVSpeed.xy;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * _Color;
            }
            ENDCG
        }
    }
}
