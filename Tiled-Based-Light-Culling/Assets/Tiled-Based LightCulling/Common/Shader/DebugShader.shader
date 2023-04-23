Shader "Code Repository/RenderPath/TBF+/Debug" 
{
	Properties 
	{

	}
	SubShader 
	{
		Tags 
		{
			"RenderPipeline"="UniversalPipeline"
		}

		Pass
		{
			Cull Off
			ZWrite Off
			ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#pragma vertex vertex
			#pragma fragment fragment

			//TEXTURE2D(_MainTex);
			//SAMPLER(sampler_MainTex);

			TEXTURE2D(_DebugTexture);
			SAMPLER(sampler_DebugTexture);
			half _DebugAlpha;

			struct Attributes 
			{
				//float4 positionOS   : POSITION;
				uint vertexID : SV_VertexID;
			};

			struct Varyings 
			{
				float4 positionCS    : SV_POSITION;
				float2 uv            : TEXCOORD0;
			};

			Varyings vertex(Attributes IN) 
			{
				Varyings OUT;
				//https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/post-processing/#3
				//OUT.positionCS = float4(IN.positionOS.xy, 0.0, 1.0);
				//OUT.uv = IN.positionOS.xy * 0.5 + 0.5;
				//https://catlikecoding.com/unity/tutorials/custom-srp/post-processing/#1.6
				OUT.positionCS = float4(IN.vertexID <= 1 ? -1.0 : 3.0, IN.vertexID == 1.0 ? 3.0 : -1.0, 0.0, 1.0);
				OUT.uv = float2(IN.vertexID <= 1 ? 0.0 : 2.0, IN.vertexID == 1 ? 2.0 : 0.0);
				#if UNITY_UV_STARTS_AT_TOP
					OUT.uv.y = 1.0 - OUT.uv.y;
				#endif
				return OUT;
			}

			half4 fragment(Varyings IN) : SV_Target 
			{
				return half4(SAMPLE_TEXTURE2D(_DebugTexture, sampler_DebugTexture, IN.uv).rgb, _DebugAlpha);
			}
			ENDHLSL
		}
	}
}