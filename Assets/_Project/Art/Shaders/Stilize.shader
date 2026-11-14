// Bilgi Adası — stilize low-poly shader (URP 17 / Unity 6)
// Renk köşe renginden (vertex color) gelir; doku yok (09 §3, §7). FBX köşe renkleri lineer yazılır, proje Linear → dönüşüm yok.
// Işık: URP'nin kendi UniversalFragmentPBR'ı (ana ışık + ek ışıklar + Forward+ + gölge + SH ortam ışığı + sis).
// Çift yüz: arka yüzde normal çevrilir (açık kenarlı parçalar delik görünmesin).
// Ek malzemeler aynı shader: M_Fener/M_Kopuk (Işıma), M_Duman/M_Cam (Saydam). Atamayı Art/Editor/SanatIceAktarma.cs yapar.
Shader "BilgiAdasi/Stilize"
{
    Properties
    {
        _Renk ("Renk çarpanı", Color) = (1, 1, 1, 1)
        _Puruzsuzluk ("Pürüzsüzlük", Range(0, 1)) = 0.12
        _Isima ("Işıma gücü (köşe rengi × güç)", Range(0, 10)) = 0
        _Opaklik ("Opaklık", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Yüz ayıklama", Float) = 0
        [Toggle(_SAYDAM)] _Saydam ("Saydam", Float) = 0
        [HideInInspector] _SrcBlend ("", Float) = 1
        [HideInInspector] _DstBlend ("", Float) = 0
        [HideInInspector] _ZWrite ("", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Renk;
            half _Puruzsuzluk;
            half _Isima;
            half _Opaklik;
            half _Cull;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------ ışıklı ana geçiş
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _SAYDAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP          // Forward+ (Unity 6.1+; _FORWARD_PLUS eskidi)
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Bake edilmiş atölye (karar 14.11): lightmap'li objeler ortam ışığını lightmap'ten alır, diğerleri SH'tan.
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #if defined(_SAYDAM)
                #define _SURFACE_TYPE_TRANSPARENT 1
            #endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half4  color      : TEXCOORD2;
                half4  fogVL      : TEXCOORD3;   // x: sis, yzw: köşe ışığı
                float2 lightmapUV : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.color = v.color;
                o.fogVL.x = ComputeFogFactor(p.positionCS.z);
                o.fogVL.yzw = VertexLighting(p.positionWS, n.normalWS);
                #if defined(LIGHTMAP_ON)
                    o.lightmapUV = v.staticLightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 nrm = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);

                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                d.positionCS = i.positionCS;
                d.normalWS = nrm;
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord = i.fogVL.x;
                d.vertexLighting = i.fogVL.yzw;
                #if defined(LIGHTMAP_ON)
                    d.bakedGI = SampleLightmap(i.lightmapUV, nrm);
                #else
                    d.bakedGI = SampleSH(nrm);
                #endif
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                half3 albedo = i.color.rgb * _Renk.rgb;
                s.albedo = albedo;
                s.metallic = 0;
                s.specular = half3(0, 0, 0);
                s.smoothness = _Puruzsuzluk;
                s.normalTS = half3(0, 0, 1);
                s.occlusion = 1;
                s.emission = albedo * _Isima;
                s.alpha = i.color.a * _Renk.a * _Opaklik;

                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                #if !defined(_SAYDAM)
                    c.a = 1;
                #endif
                return c;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------ ışık pişirme (Meta): köşe rengi = yüzey rengi,
        // köşe rengi × Işıma = ışık saçan yüzey (fener camı). Yalnızca bake sırasında kullanılır.
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertMeta
            #pragma fragment fragMeta
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct MetaAttributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
            };

            struct MetaVaryings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : TEXCOORD0;
            };

            MetaVaryings vertMeta(MetaAttributes v)
            {
                MetaVaryings o;
                o.positionCS = UnityMetaVertexPosition(v.positionOS.xyz, v.uv1, v.uv2);
                o.color = v.color;
                return o;
            }

            half4 fragMeta(MetaVaryings i) : SV_Target
            {
                UnityMetaInput m = (UnityMetaInput)0;
                half3 albedo = i.color.rgb * _Renk.rgb;
                m.Albedo = albedo;
                m.Emission = albedo * _Isima;
                return UnityMetaFragment(m);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------ gölge düşürme
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        // ------------------------------------------------------------ derinlik ön geçişi (Forward+ / derinlik dokusu)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        // ------------------------------------------------------------ derinlik + normal (SSAO vb.)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                return half4(n, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
