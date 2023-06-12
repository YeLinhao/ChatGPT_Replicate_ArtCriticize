using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XDPaint.Utils
{
    public static class ExtendedMethods
    {
        public static Vector2 Clamp(this Vector2 value, Vector2 from, Vector2 to)
        {
            if (value.x < from.x)
            {
                value.x = from.x;
            }
            if (value.y < from.y)
            {
                value.y = from.y;
            }
            if (value.x > to.x)
            {
                value.x = to.x;
            }
            if (value.y > to.y)
            {
                value.y = to.y;
            }
            return value;
        }
        
        public static bool IsNaNOrInfinity(this float value)
        {
            return float.IsInfinity(value) || float.IsNaN(value);
        }
        
        public static void ReleaseTexture(this RenderTexture renderTexture)
        {
            if (renderTexture != null && renderTexture.IsCreated())
            {
                if (RenderTexture.active == renderTexture)
                {
                    RenderTexture.active = null;
                }
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }
        
        public static Texture2D GetTexture2D(this RenderTexture renderTexture)
        {
            var format = TextureFormat.ARGB32;
            if (renderTexture.format == RenderTextureFormat.RFloat)
            {
                format = TextureFormat.RFloat;
            }
            var texture2D = new Texture2D(renderTexture.width, renderTexture.height, format, false);
            var previousRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0, false);
            texture2D.Apply();
            RenderTexture.active = previousRenderTexture;
            return texture2D;
        }

        public static string ToCamelCaseWithSpace(this string text)
        {
            return Regex.Replace(text, @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1").Trim();
        }
    }
}