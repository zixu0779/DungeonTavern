Shader "DungeonTavern/Native Pixel Face"
{
    Properties
    {
        _BaseMap("Sprite Atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _SpriteRect("Sprite Rect", Vector) = (0,0,1,1)
        _FaceUvScale("Native UV Scale", Vector) = (1,1,0,0)
        _FaceRotation("Quarter Turns", Float) = 0
        _FlipX("Flip X", Float) = 0
        _FlipY("Flip Y", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Rendering/TavernWallCutout.hlsl"
            #include "../../Rendering/TavernWallSection.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SpriteRect;
                float4 _FaceUvScale;
                float _FaceRotation;
                float _FlipX;
                float _FlipY;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float2 RotateQuarterTurns(float2 uv, float turns)
            {
                if (turns < 0.5) return uv;
                if (turns < 1.5) return float2(uv.y, 1.0 - uv.x);
                if (turns < 2.5) return 1.0 - uv;
                return float2(1.0 - uv.y, uv.x);
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC, out float depth : SV_Depth) : SV_Target
            {
                depth=input.positionCS.z;
                if (!IS_FRONT_VFACE(facing,true,false) && TavernCutCameraActive())
                {
                    float3 section=TavernSectionPoint(input.positionWS);
                    depth=TavernSectionDepth(section);
                    return TavernSectionColor(section);

                }
                else TavernWallClip(input.positionWS);
                // A 90/270 degree turn swaps the sprite's physical width and
                // height. Swap native texel density before wrapping, otherwise
                // a 4x16 sprite rotated onto a 16x4 surface samples only one
                // narrow color column.
                bool oddQuarterTurn =
                    (_FaceRotation > 0.5 && _FaceRotation < 1.5) ||
                    (_FaceRotation > 2.5);
                float2 nativeScale = oddQuarterTurn
                    ? _FaceUvScale.yx
                    : _FaceUvScale.xy;
                float2 localUv = frac(input.uv * nativeScale);
                localUv = RotateQuarterTurns(localUv, _FaceRotation);
                if (_FlipX > 0.5) localUv.x = 1.0 - localUv.x;
                if (_FlipY > 0.5) localUv.y = 1.0 - localUv.y;
                float2 atlasUv = _SpriteRect.xy + localUv * _SpriteRect.zw;
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, atlasUv) * _BaseColor;
                clip(color.a - _Cutoff);
                return color;
            }
            ENDHLSL
        }
    }
}
