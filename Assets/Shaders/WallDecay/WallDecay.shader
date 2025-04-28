Shader "Custom/WallDecay"
{
    Properties
    {
        _MainTex ("Original (Smooth) Texture", 2D) = "white" {}
        _OldTex ("Decayed Texture", 2D) = "white" {}
        _DecayProgress ("Decay Progress", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _OldTex;
        float _DecayProgress;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            fixed4 col1 = tex2D(_MainTex, uv);
            fixed4 col2 = tex2D(_OldTex, uv);
            o.Albedo = lerp(col1.rgb, col2.rgb, _DecayProgress);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}