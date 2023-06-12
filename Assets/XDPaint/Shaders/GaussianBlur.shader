Shader "XD Paint/Gaussian Blur"
{
	Properties
	{
		_MainTex ("Main", 2D) = "white" {}
		_KernelSize ("Kernel Size", Int) = 3
		_Spread ("Spread", Float) = 5.0
	}
	SubShader
	{
		ZTest Always Cull Off Zwrite Off
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;
			int _KernelSize;
			float _Spread;

			struct appdata_t
	        {
				float4 vertex   : POSITION;
	            float4 color    : COLOR;
	            float2 texcoord : TEXCOORD0;
	        };
			
	        struct v2f
	        {
				float2 texcoord  : TEXCOORD0;
	            float4 vertex   : SV_POSITION;
	            float4 color    : COLOR;
	        };

			v2f vert(appdata_t IN)
	        {
				v2f OUT;
	            OUT.vertex = UnityObjectToClipPos(IN.vertex);
	            OUT.texcoord = IN.texcoord;
	            OUT.color = IN.color;
	            return OUT;
	        }

			static const float TWO_PI = 6.28318530718;
			static const float E = 2.71828182846;
			float gaussian(int x, int y)
			{
			    float sigmaSquared = _Spread * _Spread;
			    return 1 / sqrt(TWO_PI * sigmaSquared) * pow(E, -(x * x + y * y) / (2 * sigmaSquared));
			}
			
			float4 frag(v2f i) : SV_Target
			{
				int upper = (int)((_KernelSize - 1) / 2.0f);
				int lower = -upper;
				float kernelSum = 0.0;
				float4 color = float4(0, 0, 0, 0);
				
				for (int x = lower; x <= upper; ++x)
				{
				    for (int y = lower; y <= upper; ++y)
				    {
				        float gauss = gaussian(x, y);
				        kernelSum += gauss;
				        fixed2 offset = fixed2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y);
				        color += tex2D(_MainTex, i.texcoord + offset) * gauss;
				    }
				}
				color /= kernelSum;
				return color;
			}
			ENDCG
		}
	}
}