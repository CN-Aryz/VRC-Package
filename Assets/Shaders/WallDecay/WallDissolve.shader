Shader "Custom/WallDissolveFixed"
{
    Properties
    {
        _MainTex ("Original Texture", 2D) = "white" {}
        _OldTex ("Decayed Texture", 2D) = "white" {}
        _MaskTex ("Dissolve Mask", 2D) = "gray" {}
        _DecayProgress ("Decay Progress", Range(0,1)) = 0
        _EdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        _EdgeWidth ("Edge Width", Range(0.01, 0.2)) = 0.05
        _EmissionStrength ("Emission Strength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _OldTex;
        sampler2D _MaskTex;
        float _DecayProgress;
        float _EdgeWidth;
        fixed4 _EdgeColor;
        float _EmissionStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            float maskValue = tex2D(_MaskTex, uv).r;

            // dissolve 区域判断
            float decayAmount = step(maskValue, _DecayProgress); // 0 = 未老化, 1 = 老化完成

            // 插值颜色
            fixed4 colSmooth = tex2D(_MainTex, uv);
            fixed4 colRough = tex2D(_OldTex, uv);
            o.Albedo = lerp(colSmooth.rgb, colRough.rgb, decayAmount);

            // 边缘判断
            float edge = smoothstep(_DecayProgress - _EdgeWidth, _DecayProgress, maskValue);

            // 发光强度根据进度衰减（0.8~1逐渐消失）
            float edgeFade = 1.0 - saturate((_DecayProgress - 0.8) / 0.2);

            // 最终描边
            float edgeHighlight = (1 - edge) * edgeFade;
            o.Emission = _EdgeColor.rgb * edgeHighlight * _EmissionStrength;

            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
