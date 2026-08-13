Shader "DungeonTavern/Stair Transition Fog"
{
    Properties
    {
        _Color("Fog Color", Color) = (0.025,0.03,0.045,0.96)
        _PixelDensity("Pixel Density", Float) = 32
        _Speed("Drift Speed", Float) = 0.12
        _LayerOpacity("Layer Opacity", Range(0,1)) = 1
        _NoiseOffset("Noise Offset", Vector) = (0,0,0,0)
        _DepthOverride("Depth Override", Range(-1,1)) = -1
        _CurtainMode("Curtain Mode", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _PixelDensity;
                float _Speed;
                float _LayerOpacity;
                float4 _NoiseOffset;
                float _DepthOverride;
                float _CurtainMode;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SmoothNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float2 blend = f * f * (3.0 - 2.0 * f);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _Speed;
                float2 drift = float2(time * 0.31, time * 0.17) + _NoiseOffset.xy;
                float noiseA = SmoothNoise(input.uv * (_PixelDensity * 0.42) + drift);
                float noiseB = SmoothNoise(input.uv * (_PixelDensity * 0.21) - drift * 0.63 + 7.13 + _NoiseOffset.zw);
                float noiseC = SmoothNoise(input.uv * (_PixelDensity * 0.105) + drift * 0.29 + 23.4);
                float noise = saturate(noiseA * 0.46 + noiseB * 0.34 + noiseC * 0.20);
                float largeNoise = SmoothNoise(input.uv * (_PixelDensity * 0.075) + drift * 0.37 + 19.7);
                float frontProfile = lerp(0.72, 1.0, smoothstep(0.0, 0.12, input.uv.x));
                float vertical = lerp(1.0, 0.68, input.uv.y);
                float cloudMass = smoothstep(0.28, 0.70, noise);
                float clustered = lerp(0.34, 1.0, cloudMass);
                float3 fogTone = _Color.rgb * lerp(0.48, 1.65, noise);
                // The mesh begins around 15 degrees. u=0.20 is therefore about 30 degrees:
                // fade rapidly across the player-facing portion, then stay almost black to 90 degrees.
                float topDarkness = smoothstep(0.0, 0.20, saturate(input.uv.x));
                topDarkness = _DepthOverride >= 0.0 ? _DepthOverride : topDarkness;
                float baseAlpha = _Color.a * frontProfile * vertical * clustered;
                // Noise-eroded upper/lower edges remove the hard solid-volume silhouette.
                float lowerEdge = smoothstep(0.0, 0.13, input.uv.y + (largeNoise - 0.5) * 0.16);
                float upperEdge = smoothstep(0.0, 0.24, (1.0 - input.uv.y) + (largeNoise - 0.5) * 0.28);
                float frontEdge = smoothstep(0.0, 0.11, input.uv.x + (largeNoise - 0.5) * 0.12);
                float silhouette = saturate(lowerEdge * upperEdge * frontEdge);
                float finalAlpha = lerp(baseAlpha, 0.992, topDarkness) * silhouette * _LayerOpacity;
                float3 finalColor = lerp(fogTone, float3(0.002, 0.002, 0.003), topDarkness);
                if (_CurtainMode > 0.5)
                {
                    float sideA = smoothstep(0.0, 0.28, input.uv.x) * smoothstep(-0.08, 0.18, input.uv.x + (largeNoise - 0.5) * 0.20);
                    float sideB = smoothstep(0.0, 0.28, 1.0 - input.uv.x) * smoothstep(-0.08, 0.18, (1.0 - input.uv.x) + (largeNoise - 0.5) * 0.20);
                    float curtainEdge = _CurtainMode > 1.5
                        ? saturate(lowerEdge * sideA * sideB)
                        : saturate(lowerEdge * upperEdge * sideA * sideB);
                    float curtainCloud = lerp(0.82, 1.0, cloudMass);
                    finalAlpha = 0.992 * curtainEdge * curtainCloud * _LayerOpacity;
                    finalColor = lerp(_Color.rgb * 0.72, float3(0.001, 0.001, 0.002), cloudMass);
                }
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
