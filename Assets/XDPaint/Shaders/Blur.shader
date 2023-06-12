Shader "XD Paint/Blur"
{
	Properties
	{
		_MainTex ("Main", 2D) = "white" {}
		_BlurSize ("Blur Size", Float) = 1.0
	}
	SubShader
	{
		CGINCLUDE
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		half4 _MainTex_TexelSize;
		float _BlurSize;
 
		struct v2f
		{
			float4 pos: SV_POSITION;
			half2 uv[5]: TEXCOORD0;
		};
 
		v2f vertBlurVertical(appdata_img v)
		{
			v2f output;
			output.pos = UnityObjectToClipPos(v.vertex);
			half2 uv = v.texcoord;
			output.uv[0] = uv;
			output.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
			output.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
			output.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
			output.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
			return output;
		}
 
		v2f vertBlurHorizontal(appdata_img input)
		{
			v2f output;
			output.pos = UnityObjectToClipPos(input.vertex);
			half2 uv = input.texcoord;
			output.uv[0] = uv;
			output.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
			output.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
			output.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
			output.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
			return output;
		}
			
		fixed4 fragBlur(v2f input) : SV_Target
		{
			float weight[3] = { 0.4026, 0.2442, 0.0545 };
			fixed4 sample = tex2D(_MainTex, input.uv[0]);
			fixed3 sum = sample.rgb * weight[0];
			for (int j = 1; j < 3; j++)
			{
				sum += tex2D(_MainTex, input.uv[j * 2 - 1]).rgb * weight[j];
				sum += tex2D(_MainTex, input.uv[j * 2]).rgb * weight[j];
			}
			return fixed4(sum, sample.a);
		}
		ENDCG
		
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
		ZTest Always Cull Off Zwrite Off
		Pass 
		{
			NAME "GAUSSIAN_BLUR_VERTICAL"
			CGPROGRAM
				#pragma vertex vertBlurVertical
				#pragma fragment fragBlur
			ENDCG
		}
		Pass
		{
			NAME "GAUSSIAN_BLUR_HORIZONTAL"
			CGPROGRAM
				#pragma vertex vertBlurHorizontal  
				#pragma fragment fragBlur
			ENDCG
		}
	}
	FallBack "Diffuse"
}