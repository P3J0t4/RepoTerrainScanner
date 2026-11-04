Shader "Hidden/ScanBiRP"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        scanColorHead ("scanColorHead", Color) = (0.05, 0.56, 0.85, 1)
        scanColor ("scanColor", Color) = (0.38, 0.73, 0.87, 1)
        outlineWidth ("outlineWidth", Float) = 0.0
        outlineBrightness ("outlineBrightness", Float) = 0.0
        outlineStarDistance ("outlineStarDistance", Float) = 30.0

        scanLineInterval ("scanLineInterval", Float) = 1.0
        scanLineWidth ("scanLineWidth", Float) = 1.0
        scanLineBrightness ("scanLineBrightness", Float) = 2.5
        scanRange ("scanRange", Float) = 5.0

        scanCenterWS ("scanCenterWS", Vector) = (0, 0, 0, 0)
        headScanLineDistance ("headScanLineDistance", Float) = 0.0
        headScanLineWidth ("headScanLineWidth", Float) = 1.0
        headScanLineBrightness ("headScanLineBrightness", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;
            float4x4 _InverseViewProj;

            float3 scanColorHead;
            float3 scanColor;
            float outlineWidth;
            float outlineBrightness;
            float outlineStarDistance;

            float scanLineInterval;
            float scanLineWidth;
            float scanLineBrightness;
            float scanRange;

            float4 scanCenterWS;
            float headScanLineDistance;
            float headScanLineWidth;
            float headScanLineBrightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uvs[9] : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float2 uv = v.uv;

                #if UNITY_UV_STARTS_AT_TOP
                if (_CameraDepthTexture_TexelSize.y < 0)
                    uv.y = 1.0 - uv.y;
                #endif

                float2 texel = _CameraDepthTexture_TexelSize.xy * outlineWidth;

                o.uvs[0] = uv + texel * half2(-1, 1);
                o.uvs[1] = uv + texel * half2(0, 1);
                o.uvs[2] = uv + texel * half2(1, 1);
                o.uvs[3] = uv + texel * half2(-1, 0);
                o.uvs[4] = uv;
                o.uvs[5] = uv + texel * half2(1, 0);
                o.uvs[6] = uv + texel * half2(-1, -1);
                o.uvs[7] = uv + texel * half2(0, -1);
                o.uvs[8] = uv + texel * half2(1, -1);

                return o;
            }

            half calculateVerticalOutline(float2 uvs[9])
            {
                half c = 0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[0])) * -1.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[1])) * -2.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[2])) * -1.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[6])) * 1.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[7])) * 2.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[8])) * 1.0;
                return c;
            }

            half calculateHorizontalOutline(float2 uvs[9])
            {
                half c = 0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[0])) * -1.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[3])) * -2.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[6])) * -1.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[2])) * 1.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[5])) * 2.0;
                c += Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uvs[8])) * 1.0;
                return c;
            }

            float3 ReconstructWorldPosition(float2 uv, float rawDepth)
            {
                float4 clipPos;
                clipPos.x = uv.x * 2.0 - 1.0;
                clipPos.y = uv.y * 2.0 - 1.0;
    
                #if UNITY_UV_STARTS_AT_TOP
                clipPos.y = -clipPos.y;
                #endif

                #if defined(UNITY_REVERSED_Z)
                clipPos.z = rawDepth;
                #else
                clipPos.z = rawDepth * 2.0 - 1.0;
                #endif
    
                clipPos.w = 1.0;

                float4 worldPos = mul(_InverseViewProj, clipPos);
                return worldPos.xyz / worldPos.w;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 originalColor = tex2D(_MainTex, i.uvs[4]);
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uvs[4]);

                // Excluir cielo / fondo infinito
                #if defined(UNITY_REVERSED_Z)
                if (rawDepth <= 0.00001) return originalColor;
                #else
                if (rawDepth >= 0.99999) return originalColor;
                #endif

                float3 posWS = ReconstructWorldPosition(i.uvs[4], rawDepth);
                float distanceToCenter = distance(scanCenterWS.xyz, posWS);

                // Línea frontal (Head line)
                float scanHeadLine1 = smoothstep(headScanLineDistance + 0.5 * distanceToCenter * 0.03, headScanLineDistance, distanceToCenter);
                float scanHeadLine2 = smoothstep(headScanLineDistance - headScanLineWidth * distanceToCenter * 0.2, headScanLineDistance, distanceToCenter);
                float scanHeadLine = scanHeadLine1 * scanHeadLine2 * scanHeadLine2 * scanHeadLine2 * headScanLineBrightness;
                float4 scanHeadLineColor = float4(scanColorHead * scanHeadLine, scanHeadLine);

                float scanHeadLine3 = smoothstep(headScanLineDistance - headScanLineWidth * distanceToCenter * 0.3, headScanLineDistance, distanceToCenter);
                float scanHeadLineBlack = scanHeadLine1 * scanHeadLine3 * scanHeadLine3 * scanHeadLine3 * headScanLineBrightness;
                float4 scanHeadLineColorBlack = float4(0, 0, 0, scanHeadLineBlack * 0.5);

                // Rango y líneas de barrido
                float scanLineRange2 = smoothstep(headScanLineDistance - distanceToCenter * 2.5 * scanRange, headScanLineDistance, distanceToCenter);
                float scanLineRange = scanHeadLine1 * scanLineRange2 * scanLineRange2;
                float centerFadeout = smoothstep(3.0, 6.0, distanceToCenter);

                float wave = frac(distanceToCenter / max(0.001, scanLineInterval));
                float scanLine1 = smoothstep(0.5 - scanLineWidth * distanceToCenter * 0.003, 0.5, wave);
                float scanLine2 = smoothstep(0.5 + scanLineWidth * distanceToCenter * 0.003, 0.5, wave);
                float scanLine = scanLine1 * scanLine2 * scanLineRange * scanLineBrightness * centerFadeout;
                float4 scanLineColor = float4(scanColor * scanLine, scanLine);

                // Detección de bordes (Outline)
                float4 outlineColor = float4(0, 0, 0, 0);
                if (outlineBrightness > 0.0 && outlineWidth > 0.0)
                {
                    half outlineV = calculateVerticalOutline(i.uvs);
                    half outlineH = calculateHorizontalOutline(i.uvs);
                    half outline = sqrt(outlineV * outlineV + outlineH * outlineH);
                    float depthMask = saturate(1.0 - distanceToCenter * 0.01);
                    depthMask *= depthMask;
                    half outLineDistanceMask = smoothstep(outlineStarDistance - 10.0, outlineStarDistance, distanceToCenter);
                    outline = step(1.0, outline * 1000.0 * depthMask) * outlineBrightness * scanHeadLine1 * outLineDistanceMask;
                    outlineColor = float4(scanColor * outline, outline);
                }

                float4 effectColor = scanHeadLineColor + scanHeadLineColorBlack + scanLineColor + outlineColor;
                return lerp(originalColor, originalColor + effectColor, effectColor.a);
            }
            ENDCG
        }
    }
}