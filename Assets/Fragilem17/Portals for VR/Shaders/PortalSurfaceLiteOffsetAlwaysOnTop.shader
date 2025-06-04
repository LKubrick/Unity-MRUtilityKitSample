Shader "MirrorsAndPortals/Portals/PortalSurfaceLiteOffsetAlwaysOnTop"
{
    Properties
    {
        [NoScaleOffset]_TexLeft("TexLeft", 2D) = "white" {}
        [NoScaleOffset]_TexRight("TexRight", 2D) = "white" {}
        _FadeColorBlend("FadeColorBlend", Range(0, 1)) = 0.5
        _FadeColor("FadeColor", Color) = (0, 0, 0, 0)
        _ForceEye("ForceEye", Float) = 0
        [NoScaleOffset]_Albedo("Albedo", 2D) = "black" {}
        _Tiling_And_Offset("Tiling And Offset", Vector) = (1, 1, 0, 0)
        _WorldPos("WorldPos", Vector) = (0, 0, 0, 0)
        _WorldDir("WorldDir", Vector) = (0, 0, 0, 0)
        _AlbedoAlpha("AlbedoAlpha", Float) = 1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent+1"
        }
        Pass
        {
            Name "Pass"
            Tags
            {
                // LightMode: <None>
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma shader_feature _ _SAMPLE_GI
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_UNLIT
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 WorldSpacePosition;
            float4 ScreenPosition;
            float4 uv0;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float4 interp1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }

        // 255e639040c6bb2061ca2408d67d7635
        #include "Portals.cginc"

        struct Bindings_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9
        {
            float4 ScreenPosition;
        };

        void SG_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9(UnityTexture2D _TexLeft, UnityTexture2D _TexRight, float4 _Refraction, float _forceEye, float3 _WorldPos, float3 _WorldDir, Bindings_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9 IN, out float4 OutVector4_1)
        {
            float3 _Property_3f18fdd46ed3485c90ab19298ef606bc_Out_0 = _WorldPos;
            float3 _Property_078afe0f7f9a4e73bfe84a72b61fc4cd_Out_0 = _WorldDir;
            float _Property_a2dcc460915f4aed92182b2970604a1c_Out_0 = _forceEye;
            UnityTexture2D _Property_d51b4d0cdff9481b9b30040e3e1f3f8e_Out_0 = _TexLeft;
            float4 _ScreenPosition_0b3693ff18304999ad49929ba001a472_Out_0 = float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0);
            float4 _Property_95f0a530694f445e9e23a654a1cc59a2_Out_0 = _Refraction;
            float4 _Add_492f996ce7114910b49197cd0ffe8d44_Out_2;
            Unity_Add_float4(_ScreenPosition_0b3693ff18304999ad49929ba001a472_Out_0, _Property_95f0a530694f445e9e23a654a1cc59a2_Out_0, _Add_492f996ce7114910b49197cd0ffe8d44_Out_2);
            float4 _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0 = SAMPLE_TEXTURE2D(_Property_d51b4d0cdff9481b9b30040e3e1f3f8e_Out_0.tex, _Property_d51b4d0cdff9481b9b30040e3e1f3f8e_Out_0.samplerstate, (_Add_492f996ce7114910b49197cd0ffe8d44_Out_2.xy));
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_R_4 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.r;
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_G_5 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.g;
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_B_6 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.b;
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_A_7 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.a;
            UnityTexture2D _Property_5a071538bd554f2bb7a2c35f85c767ce_Out_0 = _TexRight;
            float4 _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5a071538bd554f2bb7a2c35f85c767ce_Out_0.tex, _Property_5a071538bd554f2bb7a2c35f85c767ce_Out_0.samplerstate, (_Add_492f996ce7114910b49197cd0ffe8d44_Out_2.xy));
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_R_4 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.r;
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_G_5 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.g;
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_B_6 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.b;
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_A_7 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.a;
            float4 _StereoSwitchCustomFunction_6ffdc56b4825463383d177158eaeb08f_Out_2;
            StereoSwitch_float(_Property_3f18fdd46ed3485c90ab19298ef606bc_Out_0, _Property_078afe0f7f9a4e73bfe84a72b61fc4cd_Out_0, _Property_a2dcc460915f4aed92182b2970604a1c_Out_0, _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0, _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0, _StereoSwitchCustomFunction_6ffdc56b4825463383d177158eaeb08f_Out_2);
            OutVector4_1 = _StereoSwitchCustomFunction_6ffdc56b4825463383d177158eaeb08f_Out_2;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }

        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }

        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }

        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }

        void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }

        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_7f3d837528d24751b7b071e7ac0b5f21_Out_0 = UnityBuildTexture2DStructNoScale(_TexLeft);
            UnityTexture2D _Property_9a857fdb2beb4a67886619f7a20af47b_Out_0 = UnityBuildTexture2DStructNoScale(_TexRight);
            float _Property_3cdb1b2fff1444de90c3e51f01cc8429_Out_0 = _ForceEye;
            float3 _Property_1962ca602a2b4b35b0d454369072f11c_Out_0 = _WorldPos;
            float3 _Property_6ea94f52669f4e6c9faa49a143a6de40_Out_0 = _WorldDir;
            Bindings_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9 _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94;
            _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94.ScreenPosition = IN.ScreenPosition;
            float4 _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94_OutVector4_1;
            SG_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9(_Property_7f3d837528d24751b7b071e7ac0b5f21_Out_0, _Property_9a857fdb2beb4a67886619f7a20af47b_Out_0, float4 (0, 0, 0, 0), _Property_3cdb1b2fff1444de90c3e51f01cc8429_Out_0, _Property_1962ca602a2b4b35b0d454369072f11c_Out_0, _Property_6ea94f52669f4e6c9faa49a143a6de40_Out_0, _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94, _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94_OutVector4_1);
            float4 _Property_a5c21ec46f054211a9ccaab6b701e369_Out_0 = _FadeColor;
            float _Property_3ddbcb00b09048aeb62c78457ac26139_Out_0 = _FadeColorBlend;
            float4 _Lerp_b3d0d9df545a46cb8c47a6a66e220ab3_Out_3;
            Unity_Lerp_float4(_StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94_OutVector4_1, _Property_a5c21ec46f054211a9ccaab6b701e369_Out_0, (_Property_3ddbcb00b09048aeb62c78457ac26139_Out_0.xxxx), _Lerp_b3d0d9df545a46cb8c47a6a66e220ab3_Out_3);
            UnityTexture2D _Property_24b441e394bf4ff284322a1d7f8440e2_Out_0 = UnityBuildTexture2DStructNoScale(_Albedo);
            float4 _Property_16394dc42fe44fc983815185527be332_Out_0 = _Tiling_And_Offset;
            float _Split_d12c63adac454b34a140e1b71795383b_R_1 = _Property_16394dc42fe44fc983815185527be332_Out_0[0];
            float _Split_d12c63adac454b34a140e1b71795383b_G_2 = _Property_16394dc42fe44fc983815185527be332_Out_0[1];
            float _Split_d12c63adac454b34a140e1b71795383b_B_3 = _Property_16394dc42fe44fc983815185527be332_Out_0[2];
            float _Split_d12c63adac454b34a140e1b71795383b_A_4 = _Property_16394dc42fe44fc983815185527be332_Out_0[3];
            float2 _Vector2_11aa2d79b1bb45209ef0d6e82e44f804_Out_0 = float2(_Split_d12c63adac454b34a140e1b71795383b_R_1, _Split_d12c63adac454b34a140e1b71795383b_G_2);
            float2 _Vector2_af44ab06068744bea085c5591a7658c7_Out_0 = float2(_Split_d12c63adac454b34a140e1b71795383b_B_3, _Split_d12c63adac454b34a140e1b71795383b_A_4);
            float2 _TilingAndOffset_bcba97dd069a4811af54ff5802c16f45_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, _Vector2_11aa2d79b1bb45209ef0d6e82e44f804_Out_0, _Vector2_af44ab06068744bea085c5591a7658c7_Out_0, _TilingAndOffset_bcba97dd069a4811af54ff5802c16f45_Out_3);
            float4 _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0 = SAMPLE_TEXTURE2D(_Property_24b441e394bf4ff284322a1d7f8440e2_Out_0.tex, _Property_24b441e394bf4ff284322a1d7f8440e2_Out_0.samplerstate, _TilingAndOffset_bcba97dd069a4811af54ff5802c16f45_Out_3);
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_R_4 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.r;
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_G_5 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.g;
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_B_6 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.b;
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_A_7 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.a;
            float _Property_226aae0c28ae42828714119776c5a196_Out_0 = _AlbedoAlpha;
            float _Multiply_14a632e843dd4d0eaba4f7bc053ef236_Out_2;
            Unity_Multiply_float(_SampleTexture2D_310c2bd158664483b6b5f947792764b5_A_7, _Property_226aae0c28ae42828714119776c5a196_Out_0, _Multiply_14a632e843dd4d0eaba4f7bc053ef236_Out_2);
            float _Subtract_ecc00282058445ec9837882de5fce50e_Out_2;
            Unity_Subtract_float(_Property_226aae0c28ae42828714119776c5a196_Out_0, 1, _Subtract_ecc00282058445ec9837882de5fce50e_Out_2);
            float _Clamp_549959b467a74bdb94c8f32501295681_Out_3;
            Unity_Clamp_float(_Subtract_ecc00282058445ec9837882de5fce50e_Out_2, 0, 1, _Clamp_549959b467a74bdb94c8f32501295681_Out_3);
            float _Add_79356ef9f23c465d99778bf345f7efb8_Out_2;
            Unity_Add_float(_Multiply_14a632e843dd4d0eaba4f7bc053ef236_Out_2, _Clamp_549959b467a74bdb94c8f32501295681_Out_3, _Add_79356ef9f23c465d99778bf345f7efb8_Out_2);
            float _Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3;
            Unity_Clamp_float(_Add_79356ef9f23c465d99778bf345f7efb8_Out_2, 0, 1, _Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3);
            float _OneMinus_b991c26203d04e349e763f17c2cda40e_Out_1;
            Unity_OneMinus_float(_Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3, _OneMinus_b991c26203d04e349e763f17c2cda40e_Out_1);
            float4 _Multiply_ba024f116c794c5f8410e7772f90721e_Out_2;
            Unity_Multiply_float(_Lerp_b3d0d9df545a46cb8c47a6a66e220ab3_Out_3, (_OneMinus_b991c26203d04e349e763f17c2cda40e_Out_1.xxxx), _Multiply_ba024f116c794c5f8410e7772f90721e_Out_2);
            float4 _Combine_7dcc774a66124aaf99cffa14397e2f35_RGBA_4;
            float3 _Combine_7dcc774a66124aaf99cffa14397e2f35_RGB_5;
            float2 _Combine_7dcc774a66124aaf99cffa14397e2f35_RG_6;
            Unity_Combine_float(_SampleTexture2D_310c2bd158664483b6b5f947792764b5_R_4, _SampleTexture2D_310c2bd158664483b6b5f947792764b5_G_5, _SampleTexture2D_310c2bd158664483b6b5f947792764b5_B_6, 1, _Combine_7dcc774a66124aaf99cffa14397e2f35_RGBA_4, _Combine_7dcc774a66124aaf99cffa14397e2f35_RGB_5, _Combine_7dcc774a66124aaf99cffa14397e2f35_RG_6);
            float3 _Multiply_5a65d16ff89040daa9644c2529b30f46_Out_2;
            Unity_Multiply_float(_Combine_7dcc774a66124aaf99cffa14397e2f35_RGB_5, (_Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3.xxx), _Multiply_5a65d16ff89040daa9644c2529b30f46_Out_2);
            float3 _Add_5abe626d8bff498c8aaa6541e8c6974f_Out_2;
            Unity_Add_float3((_Multiply_ba024f116c794c5f8410e7772f90721e_Out_2.xyz), _Multiply_5a65d16ff89040daa9644c2529b30f46_Out_2, _Add_5abe626d8bff498c8aaa6541e8c6974f_Out_2);
            surface.BaseColor = _Add_5abe626d8bff498c8aaa6541e8c6974f_Out_2;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





            output.WorldSpacePosition =          input.positionWS;
            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
            output.uv0 =                         input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_NORMAL_WS
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS;
            float4 tangentWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float4 interp1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

            ENDHLSL
        }
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent+1"
        }
        Pass
        {
            Name "Pass"
            Tags
            {
                // LightMode: <None>
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma shader_feature _ _SAMPLE_GI
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_UNLIT
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS;
            float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            float3 WorldSpacePosition;
            float4 ScreenPosition;
            float4 uv0;
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float4 interp1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }

        // 255e639040c6bb2061ca2408d67d7635
        #include "Portals.cginc"

        struct Bindings_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9
        {
            float4 ScreenPosition;
        };

        void SG_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9(UnityTexture2D _TexLeft, UnityTexture2D _TexRight, float4 _Refraction, float _forceEye, float3 _WorldPos, float3 _WorldDir, Bindings_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9 IN, out float4 OutVector4_1)
        {
            float3 _Property_3f18fdd46ed3485c90ab19298ef606bc_Out_0 = _WorldPos;
            float3 _Property_078afe0f7f9a4e73bfe84a72b61fc4cd_Out_0 = _WorldDir;
            float _Property_a2dcc460915f4aed92182b2970604a1c_Out_0 = _forceEye;
            UnityTexture2D _Property_d51b4d0cdff9481b9b30040e3e1f3f8e_Out_0 = _TexLeft;
            float4 _ScreenPosition_0b3693ff18304999ad49929ba001a472_Out_0 = float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0);
            float4 _Property_95f0a530694f445e9e23a654a1cc59a2_Out_0 = _Refraction;
            float4 _Add_492f996ce7114910b49197cd0ffe8d44_Out_2;
            Unity_Add_float4(_ScreenPosition_0b3693ff18304999ad49929ba001a472_Out_0, _Property_95f0a530694f445e9e23a654a1cc59a2_Out_0, _Add_492f996ce7114910b49197cd0ffe8d44_Out_2);
            float4 _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0 = SAMPLE_TEXTURE2D(_Property_d51b4d0cdff9481b9b30040e3e1f3f8e_Out_0.tex, _Property_d51b4d0cdff9481b9b30040e3e1f3f8e_Out_0.samplerstate, (_Add_492f996ce7114910b49197cd0ffe8d44_Out_2.xy));
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_R_4 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.r;
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_G_5 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.g;
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_B_6 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.b;
            float _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_A_7 = _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0.a;
            UnityTexture2D _Property_5a071538bd554f2bb7a2c35f85c767ce_Out_0 = _TexRight;
            float4 _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5a071538bd554f2bb7a2c35f85c767ce_Out_0.tex, _Property_5a071538bd554f2bb7a2c35f85c767ce_Out_0.samplerstate, (_Add_492f996ce7114910b49197cd0ffe8d44_Out_2.xy));
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_R_4 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.r;
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_G_5 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.g;
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_B_6 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.b;
            float _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_A_7 = _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0.a;
            float4 _StereoSwitchCustomFunction_6ffdc56b4825463383d177158eaeb08f_Out_2;
            StereoSwitch_float(_Property_3f18fdd46ed3485c90ab19298ef606bc_Out_0, _Property_078afe0f7f9a4e73bfe84a72b61fc4cd_Out_0, _Property_a2dcc460915f4aed92182b2970604a1c_Out_0, _SampleTexture2D_1ba32d2f195d47cf948d350453a70776_RGBA_0, _SampleTexture2D_f3fba58045cc40a9b07a1b9525ca7459_RGBA_0, _StereoSwitchCustomFunction_6ffdc56b4825463383d177158eaeb08f_Out_2);
            OutVector4_1 = _StereoSwitchCustomFunction_6ffdc56b4825463383d177158eaeb08f_Out_2;
        }

        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }

        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }

        void Unity_Multiply_float(float A, float B, out float Out)
        {
            Out = A * B;
        }

        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }

        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }

        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }

        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }

        void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }

        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }

        void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }

        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_7f3d837528d24751b7b071e7ac0b5f21_Out_0 = UnityBuildTexture2DStructNoScale(_TexLeft);
            UnityTexture2D _Property_9a857fdb2beb4a67886619f7a20af47b_Out_0 = UnityBuildTexture2DStructNoScale(_TexRight);
            float _Property_3cdb1b2fff1444de90c3e51f01cc8429_Out_0 = _ForceEye;
            float3 _Property_1962ca602a2b4b35b0d454369072f11c_Out_0 = _WorldPos;
            float3 _Property_6ea94f52669f4e6c9faa49a143a6de40_Out_0 = _WorldDir;
            Bindings_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9 _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94;
            _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94.ScreenPosition = IN.ScreenPosition;
            float4 _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94_OutVector4_1;
            SG_StereoTexturesInScreenPosPortals_36ec4452c207dc54baee14adb72d8de9(_Property_7f3d837528d24751b7b071e7ac0b5f21_Out_0, _Property_9a857fdb2beb4a67886619f7a20af47b_Out_0, float4 (0, 0, 0, 0), _Property_3cdb1b2fff1444de90c3e51f01cc8429_Out_0, _Property_1962ca602a2b4b35b0d454369072f11c_Out_0, _Property_6ea94f52669f4e6c9faa49a143a6de40_Out_0, _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94, _StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94_OutVector4_1);
            float4 _Property_a5c21ec46f054211a9ccaab6b701e369_Out_0 = _FadeColor;
            float _Property_3ddbcb00b09048aeb62c78457ac26139_Out_0 = _FadeColorBlend;
            float4 _Lerp_b3d0d9df545a46cb8c47a6a66e220ab3_Out_3;
            Unity_Lerp_float4(_StereoTexturesInScreenPosPortals_3995f2cfd1b8453b953abce0d1e1bf94_OutVector4_1, _Property_a5c21ec46f054211a9ccaab6b701e369_Out_0, (_Property_3ddbcb00b09048aeb62c78457ac26139_Out_0.xxxx), _Lerp_b3d0d9df545a46cb8c47a6a66e220ab3_Out_3);
            UnityTexture2D _Property_24b441e394bf4ff284322a1d7f8440e2_Out_0 = UnityBuildTexture2DStructNoScale(_Albedo);
            float4 _Property_16394dc42fe44fc983815185527be332_Out_0 = _Tiling_And_Offset;
            float _Split_d12c63adac454b34a140e1b71795383b_R_1 = _Property_16394dc42fe44fc983815185527be332_Out_0[0];
            float _Split_d12c63adac454b34a140e1b71795383b_G_2 = _Property_16394dc42fe44fc983815185527be332_Out_0[1];
            float _Split_d12c63adac454b34a140e1b71795383b_B_3 = _Property_16394dc42fe44fc983815185527be332_Out_0[2];
            float _Split_d12c63adac454b34a140e1b71795383b_A_4 = _Property_16394dc42fe44fc983815185527be332_Out_0[3];
            float2 _Vector2_11aa2d79b1bb45209ef0d6e82e44f804_Out_0 = float2(_Split_d12c63adac454b34a140e1b71795383b_R_1, _Split_d12c63adac454b34a140e1b71795383b_G_2);
            float2 _Vector2_af44ab06068744bea085c5591a7658c7_Out_0 = float2(_Split_d12c63adac454b34a140e1b71795383b_B_3, _Split_d12c63adac454b34a140e1b71795383b_A_4);
            float2 _TilingAndOffset_bcba97dd069a4811af54ff5802c16f45_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, _Vector2_11aa2d79b1bb45209ef0d6e82e44f804_Out_0, _Vector2_af44ab06068744bea085c5591a7658c7_Out_0, _TilingAndOffset_bcba97dd069a4811af54ff5802c16f45_Out_3);
            float4 _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0 = SAMPLE_TEXTURE2D(_Property_24b441e394bf4ff284322a1d7f8440e2_Out_0.tex, _Property_24b441e394bf4ff284322a1d7f8440e2_Out_0.samplerstate, _TilingAndOffset_bcba97dd069a4811af54ff5802c16f45_Out_3);
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_R_4 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.r;
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_G_5 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.g;
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_B_6 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.b;
            float _SampleTexture2D_310c2bd158664483b6b5f947792764b5_A_7 = _SampleTexture2D_310c2bd158664483b6b5f947792764b5_RGBA_0.a;
            float _Property_226aae0c28ae42828714119776c5a196_Out_0 = _AlbedoAlpha;
            float _Multiply_14a632e843dd4d0eaba4f7bc053ef236_Out_2;
            Unity_Multiply_float(_SampleTexture2D_310c2bd158664483b6b5f947792764b5_A_7, _Property_226aae0c28ae42828714119776c5a196_Out_0, _Multiply_14a632e843dd4d0eaba4f7bc053ef236_Out_2);
            float _Subtract_ecc00282058445ec9837882de5fce50e_Out_2;
            Unity_Subtract_float(_Property_226aae0c28ae42828714119776c5a196_Out_0, 1, _Subtract_ecc00282058445ec9837882de5fce50e_Out_2);
            float _Clamp_549959b467a74bdb94c8f32501295681_Out_3;
            Unity_Clamp_float(_Subtract_ecc00282058445ec9837882de5fce50e_Out_2, 0, 1, _Clamp_549959b467a74bdb94c8f32501295681_Out_3);
            float _Add_79356ef9f23c465d99778bf345f7efb8_Out_2;
            Unity_Add_float(_Multiply_14a632e843dd4d0eaba4f7bc053ef236_Out_2, _Clamp_549959b467a74bdb94c8f32501295681_Out_3, _Add_79356ef9f23c465d99778bf345f7efb8_Out_2);
            float _Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3;
            Unity_Clamp_float(_Add_79356ef9f23c465d99778bf345f7efb8_Out_2, 0, 1, _Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3);
            float _OneMinus_b991c26203d04e349e763f17c2cda40e_Out_1;
            Unity_OneMinus_float(_Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3, _OneMinus_b991c26203d04e349e763f17c2cda40e_Out_1);
            float4 _Multiply_ba024f116c794c5f8410e7772f90721e_Out_2;
            Unity_Multiply_float(_Lerp_b3d0d9df545a46cb8c47a6a66e220ab3_Out_3, (_OneMinus_b991c26203d04e349e763f17c2cda40e_Out_1.xxxx), _Multiply_ba024f116c794c5f8410e7772f90721e_Out_2);
            float4 _Combine_7dcc774a66124aaf99cffa14397e2f35_RGBA_4;
            float3 _Combine_7dcc774a66124aaf99cffa14397e2f35_RGB_5;
            float2 _Combine_7dcc774a66124aaf99cffa14397e2f35_RG_6;
            Unity_Combine_float(_SampleTexture2D_310c2bd158664483b6b5f947792764b5_R_4, _SampleTexture2D_310c2bd158664483b6b5f947792764b5_G_5, _SampleTexture2D_310c2bd158664483b6b5f947792764b5_B_6, 1, _Combine_7dcc774a66124aaf99cffa14397e2f35_RGBA_4, _Combine_7dcc774a66124aaf99cffa14397e2f35_RGB_5, _Combine_7dcc774a66124aaf99cffa14397e2f35_RG_6);
            float3 _Multiply_5a65d16ff89040daa9644c2529b30f46_Out_2;
            Unity_Multiply_float(_Combine_7dcc774a66124aaf99cffa14397e2f35_RGB_5, (_Clamp_85beb48bb5d849bfa573be2c6f6161ca_Out_3.xxx), _Multiply_5a65d16ff89040daa9644c2529b30f46_Out_2);
            float3 _Add_5abe626d8bff498c8aaa6541e8c6974f_Out_2;
            Unity_Add_float3((_Multiply_ba024f116c794c5f8410e7772f90721e_Out_2.xyz), _Multiply_5a65d16ff89040daa9644c2529b30f46_Out_2, _Add_5abe626d8bff498c8aaa6541e8c6974f_Out_2);
            surface.BaseColor = _Add_5abe626d8bff498c8aaa6541e8c6974f_Out_2;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





            output.WorldSpacePosition =          input.positionWS;
            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
            output.uv0 =                         input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_NORMAL_WS
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20
        ColorMask 0

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // Render State
            Cull Back
        Blend One Zero
        ZTest Always
        ZWrite On
        Offset -2, -20

            // Debug
            // <None>

            // --------------------------------------------------
            // Pass

            HLSLPROGRAM

            // Pragmas
            #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

            // DotsInstancingOptions: <None>
            // HybridV1InjectedBuiltinProperties: <None>

            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>

            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
            /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS;
            float4 tangentWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
            float3 ObjectSpaceNormal;
            float3 ObjectSpaceTangent;
            float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 interp0 : TEXCOORD0;
            float4 interp1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };

            PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }

            // --------------------------------------------------
            // Graph

            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexLeft_TexelSize;
        float4 _TexRight_TexelSize;
        float _FadeColorBlend;
        float4 _FadeColor;
        float _ForceEye;
        float4 _Albedo_TexelSize;
        float4 _Tiling_And_Offset;
        float3 _WorldPos;
        float3 _WorldDir;
        float _AlbedoAlpha;
        CBUFFER_END

        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexLeft);
        SAMPLER(sampler_TexLeft);
        TEXTURE2D(_TexRight);
        SAMPLER(sampler_TexRight);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

            // Graph Functions
            // GraphFunctions: <None>

            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };

        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }

            // Graph Pixel
            struct SurfaceDescription
        {
        };

        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }

            // --------------------------------------------------
            // Build Graph Inputs

            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);

            output.ObjectSpaceNormal =           input.normalOS;
            output.ObjectSpaceTangent =          input.tangentOS.xyz;
            output.ObjectSpacePosition =         input.positionOS;

            return output;
        }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

            return output;
        }

            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

            ENDHLSL
        }
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}