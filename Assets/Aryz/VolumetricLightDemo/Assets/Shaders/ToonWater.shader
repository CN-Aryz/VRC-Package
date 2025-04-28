Shader "Custom/ToonWater"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}

        _ColorShallow ("Shallow Water Color", Color) = (0.3, 0.8, 1.0, 0.7)
        _ColorDeep ("Deep Water Color", Color) = (0.0, 0.2, 0.5, 0.7)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)

        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.1
        _WaveSpeed ("Wave Speed", Range(0.1, 10)) = 1.0
        _UVFlow ("UV Flow Speed", Vector) = (0.05, 0.02, 0, 0)

        _DepthRange ("Depth Fade Distance", Range(0.1, 5)) = 1.0
        _FoamCutoff ("Foam Threshold", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 300
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
            sampler2D _NormalMap;
            sampler2D _FoamTex;
            sampler2D_float _CameraDepthTexture; // ✅ 手动声明深度贴图

            float4 _MainTex_ST;
            float4 _ColorShallow, _ColorDeep, _FoamColor;
            float _WaveStrength, _WaveSpeed;
            float4 _UVFlow;
            float _DepthRange, _FoamCutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * 4.0 + v.vertex.z * 4.0);
                v.vertex.y += wave * _WaveStrength;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.screenPos = ComputeScreenPos(o.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uvFlow = i.uv + _Time.y * _UVFlow.xy;
                fixed4 texColor = tex2D(_MainTex, uvFlow);
                fixed3 normal = UnpackNormal(tex2D(_NormalMap, uvFlow));

                float3 lightDir = normalize(float3(0.3, 1, 0.2));
                float light = saturate(dot(i.normal + normal, lightDir));

                // 深度采样
                float rawDepth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
                float sceneZ = LinearEyeDepth(rawDepth);
                float waterZ = i.screenPos.w;
                float depth = saturate((sceneZ - waterZ) / _DepthRange);

                // 水体颜色渐变
                fixed4 waterColor = lerp(_ColorShallow, _ColorDeep, depth);

                // 泡沫处理
                float foam = tex2D(_FoamTex, i.uv * 4.0).r;
                float foamFactor = smoothstep(_FoamCutoff, 1.0, foam * depth);
                fixed4 foamColor = _FoamColor * foamFactor;

                // 最终混合
                fixed4 finalColor = lerp(waterColor, foamColor, foamFactor);
                finalColor.rgb *= light;
                finalColor.a = waterColor.a;

                return finalColor;
            }
            ENDCG
        }
    }
}
