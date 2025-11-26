Shader "Custom/VertexColorVR"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"  }
        LOD 100
        Cull Off

        Pass
        {
        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1; 
                UNITY_VERTEX_OUTPUT_STEREO
            };


            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _ShadowIntensity;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                              
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;        
                OUT.color = IN.color;
              
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = _Color;

                col *= IN.color;

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadow = mainLight.shadowAttenuation;
                
                col.rgb *= lerp(1.0 - _ShadowIntensity, 1.0, shadow);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}