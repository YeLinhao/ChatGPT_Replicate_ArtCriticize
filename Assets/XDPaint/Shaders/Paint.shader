Shader "XD Paint/Paint"
{
    Properties
    {
        _MainTex ("Main", 2D) = "white" {}
        _Input ("Input", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        _Brush ("Brush", 2D) = "white" {}
        _BrushOffset ("Brush offset", Vector) = (0, 0, 0, 0)
        _Color ("Main Color", Color) = (1, 1, 1, 1)
    	_Opacity ("Opacity", float) = 1
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        Cull Off
    	Lighting Off 
    	ZWrite Off
        ZTest Always
    	Fog { Color (0, 0, 0, 0) }
    	Blend Off
        Pass
        {
        	//Paint
            CGINCLUDE
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _Input;
			sampler2D _Mask;
            float4 _Color;
            float _Opacity;
			ENDCG

			CGPROGRAM

			#include "BlendingModes.cginc"
			#pragma multi_compile XDPAINT_LAYER_BLEND_NORMAL XDPAINT_LAYER_BLEND_DARKEN XDPAINT_LAYER_BLEND_MULTIPLY XDPAINT_LAYER_BLEND_COLORBURN XDPAINT_LAYER_BLEND_LINEARBURN XDPAINT_LAYER_BLEND_DARKERCOLOR XDPAINT_LAYER_BLEND_LIGHTEN XDPAINT_LAYER_BLEND_SCREEN XDPAINT_LAYER_BLEND_COLORDODGE XDPAINT_LAYER_BLEND_LINEARDODGE XDPAINT_LAYER_BLEND_LIGHTERCOLOR XDPAINT_LAYER_BLEND_OVERLAY XDPAINT_LAYER_BLEND_SOFTLIGHT XDPAINT_LAYER_BLEND_HARDLIGHT XDPAINT_LAYER_BLEND_VIVIDLIGHT XDPAINT_LAYER_BLEND_LINEARLIGHT XDPAINT_LAYER_BLEND_PINLIGHT XDPAINT_LAYER_BLEND_HARDMIX XDPAINT_LAYER_BLEND_DIFFERENCE XDPAINT_LAYER_BLEND_EXCLUSION XDPAINT_LAYER_BLEND_SUBTRACT XDPAINT_LAYER_BLEND_DIVIDE XDPAINT_LAYER_BLEND_HUE XDPAINT_LAYER_BLEND_SATURATION XDPAINT_LAYER_BLEND_COLOR XDPAINT_LAYER_BLEND_LUMINOSITY
			
			#pragma vertex vert
			#pragma fragment frag3

			float4 frag3 (v2f i) : SV_Target
			{
				float4 layer = tex2D(_MainTex, i.uv) * _Color;
				layer.a *= tex2D(_Mask, i.uv).r;
				float4 combined = tex2D(_Input, i.uv);
#ifdef XDPAINT_LAYER_BLEND_NORMAL
				float4 color = AlphaComposite(combined, combined.a, layer, _Opacity * layer.a);
#else
				float4 color = 1;
				color.rgb = XDPAINT_LAYER_BLEND(layer, combined);
				color.a = layer.a;
				color = AlphaComposite(combined, combined.a, color, _Opacity * layer.a);
#endif
				return color;
			}
			ENDCG
        }
        Pass
        {
        	//Blend
			CGPROGRAM

			#include "BlendingModes.cginc"
			#pragma multi_compile XDPAINT_LAYER_BLEND_NORMAL XDPAINT_LAYER_BLEND_DARKEN XDPAINT_LAYER_BLEND_MULTIPLY XDPAINT_LAYER_BLEND_COLORBURN XDPAINT_LAYER_BLEND_LINEARBURN XDPAINT_LAYER_BLEND_DARKERCOLOR XDPAINT_LAYER_BLEND_LIGHTEN XDPAINT_LAYER_BLEND_SCREEN XDPAINT_LAYER_BLEND_COLORDODGE XDPAINT_LAYER_BLEND_LINEARDODGE XDPAINT_LAYER_BLEND_LIGHTERCOLOR XDPAINT_LAYER_BLEND_OVERLAY XDPAINT_LAYER_BLEND_SOFTLIGHT XDPAINT_LAYER_BLEND_HARDLIGHT XDPAINT_LAYER_BLEND_VIVIDLIGHT XDPAINT_LAYER_BLEND_LINEARLIGHT XDPAINT_LAYER_BLEND_PINLIGHT XDPAINT_LAYER_BLEND_HARDMIX XDPAINT_LAYER_BLEND_DIFFERENCE XDPAINT_LAYER_BLEND_EXCLUSION XDPAINT_LAYER_BLEND_SUBTRACT XDPAINT_LAYER_BLEND_DIVIDE XDPAINT_LAYER_BLEND_HUE XDPAINT_LAYER_BLEND_SATURATION XDPAINT_LAYER_BLEND_COLOR XDPAINT_LAYER_BLEND_LUMINOSITY
			#pragma vertex vert
			#pragma fragment frag
			
			float4 frag (v2f i) : SV_Target
			{
				float4 paintColor = tex2D(_MainTex, i.uv);
				float4 inputColor = tex2D(_Input, i.uv);
				float4 color = AlphaComposite(paintColor, paintColor.a, inputColor, inputColor.a);
				return color;
			}
			ENDCG
        }
        Pass
        {
        	//Erase
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag2

			float4 frag2 (v2f i) : SV_Target
			{
				float4 paintColor = tex2D(_MainTex, i.uv);
				float4 inputColor = tex2D(_Input, i.uv);
				paintColor.a -= paintColor.a * inputColor.a;
				return paintColor;
			}
			ENDCG
        }
        Pass
        {
        	//Preview
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
			#include "BlendingModes.cginc"

            sampler2D _Brush;
            float4 _BrushOffset;

            float4 frag (v2f_img i) : SV_Target
            {
            	float4 paintColor = tex2D(_MainTex, i.uv);
                float4 result = tex2D(_Brush, float2(i.uv.x * _BrushOffset.z - _BrushOffset.x + 0.5f, i.uv.y * _BrushOffset.w - _BrushOffset.y + 0.5f)) * _Color;
            	float4 color = AlphaComposite(paintColor, paintColor.a, result, result.a);
                return color;
            }
            ENDCG
        }
    }
}