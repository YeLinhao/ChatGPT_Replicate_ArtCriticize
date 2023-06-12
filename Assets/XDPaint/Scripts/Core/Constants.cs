using UnityEngine;

namespace XDPaint.Core
{
    public static class Constants
    {
        public static class Defines
        {
            public static readonly string[] VREnabled = { "XDPAINT_VR_ENABLE" };
        }
        
        public static class Color
        {
            public static readonly UnityEngine.Color ClearBlack = new UnityEngine.Color(0, 0, 0, 0);
            public static readonly UnityEngine.Color ClearWhite = new UnityEngine.Color(1, 1, 1, 0);
        }

        public static class PaintShader
        {
            public const string PaintTexture = "_MainTex";
            public const string InputTexture = "_Input";
            public const string MaskTexture = "_Mask";
            public const string BrushTexture = "_Brush";
            public const string BrushOffset = "_BrushOffset";
            public const string Opacity = "_Opacity";
            public const string LayerBlendFormat = "XDPAINT_LAYER_BLEND_{0}";
        }

        public static class BrushShader
        {
            public const string SrcColorBlend = "_SrcColorBlend";
            public const string DstColorBlend = "_DstColorBlend";
            public const string SrcAlphaBlend = "_SrcAlphaBlend";
            public const string DstAlphaBlend = "_DstAlphaBlend";
            public const string BlendOpColor = "_BlendOpColor";
            public const string BlendOpAlpha = "_BlendOpAlpha";
            public const string Hardness = "_Hardness";
            public const string TexelSize = "_TexelSize";
            public const string ScaleUV = "_ScaleUV";
            public const string Offset = "_Offset";
            public const int RenderTexturePadding = 2;
        }
        
        public static class EyedropperShader
        {
            public static readonly int BrushTexture = Shader.PropertyToID("_BrushTex");
            public static readonly int BrushOffset = Shader.PropertyToID("_BrushOffset");
        }

        public static class BrushSamplerShader
        {
            public static readonly int BrushTexture = Shader.PropertyToID("_BrushTex");
            public static readonly int BrushMaskTexture = Shader.PropertyToID("_BrushMaskTex");
            public static readonly int BrushOffset = Shader.PropertyToID("_BrushOffset");
        }
        
        public static class CloneShader
        {
            public static readonly int MaskTexture = Shader.PropertyToID("_MaskTex");
            public static readonly int Offset = Shader.PropertyToID("_Offset");
        }

        public static class BlurShader
        {
            public static readonly int MaskTexture = Shader.PropertyToID("_MaskTex");
            public static readonly int BlurSize = Shader.PropertyToID("_BlurSize");
        }
        
        public static class GaussianBlurShader
        {
            public static readonly int Size = Shader.PropertyToID("_KernelSize");
            public static readonly int Spread = Shader.PropertyToID("_Spread");
            public static readonly int MaskTexture = Shader.PropertyToID("_MaskTex");
        }
        
        public static class GrayscaleShader
        {
            public static readonly int MaskTexture = Shader.PropertyToID("_MaskTex");
        }
    }
}