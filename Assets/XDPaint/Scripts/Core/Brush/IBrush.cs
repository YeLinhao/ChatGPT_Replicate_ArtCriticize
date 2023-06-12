using System;
using UnityEngine;

namespace XDPaint.Core.Materials
{
    public interface IBrush
    {
        event Action<Color> OnColorChanged;
        event Action<Texture> OnTextureChanged;
        event Action<bool> OnPreviewChanged;
        
        string Name { get; set; } 
        Material Material { get; }
        bool Preview { get; set; }
        float Hardness { get; set; }
        float Size { get; }
        Vector2 RenderOffset { get; }
        FilterMode FilterMode { get; }
        Color Color { get; }
        Texture SourceTexture { get; set; }
        RenderTexture RenderTexture { get; }

        void SetColor(Color color, bool render = true, bool sendToEvent = true);
        void SetTexture(Texture texture, bool render = true, bool sendToEvent = true, bool canUpdateRenderTexture = true);
        void RenderFromTexture(Texture texture);
        void Render();
    }
}