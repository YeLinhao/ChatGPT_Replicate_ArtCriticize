Shader "XD Paint/Average Color"
{
    Properties {
        _MainTex ("Main", 2D) = "white" {}
        _Accuracy ("Accuracy", Int) = 64
    }

    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        ZWrite Off
        ZTest Off
        Lighting Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            uniform float4 _MainTex_ST;
            float _Accuracy;

            struct app2vert
            {
                float4 position: POSITION;
                float4 color: COLOR;
                float2 texcoord: TEXCOORD0;
            };

            struct vert2frag
            {
                float4 position: SV_POSITION;
                float4 color: COLOR;
                float2 texcoord: TEXCOORD0;
            };

            vert2frag vert(app2vert input)
            {
                vert2frag output;
                output.position = UnityObjectToClipPos(input.position);
				output.color = input.color;
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                return output;
            }

            float4 frag(vert2frag input) : COLOR
            {
                float4 averageColor = float4(0, 0, 0, 0);
                float texWidth = (int)(_MainTex_TexelSize.z / _Accuracy);
                float texHeight = (int)(_MainTex_TexelSize.w / _Accuracy);
                float div = texWidth * texHeight;
                //sampling RenderTexture to get average color value
                for	(int i = 0; i < texWidth; i++)
                {
                	for	(int j = 0; j < texHeight; j++)
                	{
                		float2 newCoord = float2(i / (texWidth - 1.0f), j / (texHeight - 1.0f));
                		averageColor += tex2D(_MainTex, newCoord);
                	}
                }
                averageColor /= div;
                return averageColor;
            }
            ENDCG
        }
    }
}
