Shader "XD Paint/Grayscale"
{
    Properties
    {
        _MainTex ("Main", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "PreviewType"="Plane"}
        Cull Off Lighting Off ZTest Off ZWrite Off Fog { Color (0, 0, 0, 0) }
        Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
 
            sampler2D _MainTex;
            sampler2D _MaskTex;

            float4 frag (v2f_img i) : SV_Target
            {
                float4 paintColor = tex2D(_MainTex, i.uv);
                float4 inputColor = tex2D(_MaskTex, i.uv);
                paintColor.rgb = dot(paintColor.rgb, float3(0.3, 0.59, 0.11));
                paintColor.a *= inputColor.a;
	    		return paintColor;
            }
            ENDCG
        }
    }
}