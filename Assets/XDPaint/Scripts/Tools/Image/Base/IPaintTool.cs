using UnityEngine;
using UnityEngine.Rendering;
using XDPaint.Core;
using XDPaint.Core.Materials;
using XDPaint.Core.PaintModes;
using IDisposable = XDPaint.Core.IDisposable;

namespace XDPaint.Tools.Image.Base
{
    public interface IPaintTool : IDisposable
    {
        PaintTool Type { get; }
        bool AllowRender { get; }
        bool CanDrawLines { get; }
        bool ConsiderPreviousPosition { get; }
        bool ShowPreview { get; }
        bool RenderToTextures { get; }
        bool RenderToLayer { get; }
        bool RenderToInput { get; }
        bool DrawPreProcess { get; }
        bool DrawProcess { get; }
        bool BakeInputToPaint { get; }

        void FillWithColor(Color color);
        void SetPaintMode(IPaintMode mode);
        void OnBrushChanged(IBrush brush);
        void OnDrawPreProcess(RenderTargetIdentifier combined);
        void OnDrawProcess(RenderTargetIdentifier combined);
        void OnBakeInputToLayer(RenderTargetIdentifier activeLayer);
        void Enter();
        void Exit();
        void UpdateHover(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure);
        void UpdateDown(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure);
        void UpdatePress(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure);
        void UpdateUp(Vector2 screenPosition, bool inBounds);
    }
}