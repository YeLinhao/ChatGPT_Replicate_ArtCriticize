using UnityEngine;
using XDPaint.Core.PaintObject.Base;

namespace XDPaint.Core.PaintObject
{
    public sealed class CanvasRendererPaint : BasePaintObject
    {
        private Canvas canvas;
        private RectTransform rectTransform;
        private Vector2 objectBoundsSize;
        private RenderMode renderMode;

        protected override void Init()
        {
            canvas = ObjectTransform.transform.GetComponentInParent<Canvas>();
            ObjectTransform.TryGetComponent(out rectTransform);
            UpdateObjectBounds();
        }

        protected override bool IsInBounds(Vector3 position)
        {
            Vector2 clickPosition = position;
            var bounds = new Bounds(rectTransform.position, Vector2.Scale(rectTransform.rect.size, ObjectTransform.lossyScale));
            bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0);
            if (renderMode == RenderMode.ScreenSpaceOverlay)
            {
                bounds.size += new Vector3(Brush.RenderTexture.width, Brush.RenderTexture.height);
            }
            else
            {
                var offset = new Vector3(
                    Brush.RenderTexture.width * Brush.Size / PaintMaterial.SourceTexture.width * bounds.size.x,
                    Brush.RenderTexture.height * Brush.Size/ PaintMaterial.SourceTexture.height * bounds.size.y);
                bounds.center = rectTransform.position;
                bounds.size += offset;
                var ray = Camera.ScreenPointToRay(clickPosition);
                return bounds.IntersectRay(ray);
            }
            return bounds.Contains(clickPosition);
        }

        private void UpdateObjectBounds()
        {
            if (rectTransform != null)
            {
                var rect = rectTransform.rect;
                var lossyScale = rectTransform.lossyScale;
                objectBoundsSize = new Vector2(rect.size.x * lossyScale.x, rect.size.y * lossyScale.y);
                if (canvas != null)
                {
                    renderMode = canvas.renderMode;
                }
                else
                {
                    Debug.LogWarning("Can't find Canvas component in parent GameObjects!");
                }
            }
        }

        protected override void CalculatePaintPosition(Vector3 position, Vector2? uv = null, bool usePostPaint = true)
        {
            InBounds = IsInBounds(position);
            if (InBounds)
            {
                IsPaintingDone = true;
            }
            
            Vector3 clickPosition;
            if (renderMode == RenderMode.ScreenSpaceOverlay)
            {
                clickPosition = position;
            }
            else
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, position, Camera, out clickPosition);
            }

            var surfaceLocalClickPosition = ObjectTransform.InverseTransformPoint(clickPosition);
            var lossyScale = ObjectTransform.lossyScale;
            var clickLocalPosition = new Vector2(surfaceLocalClickPosition.x * lossyScale.x, surfaceLocalClickPosition.y * lossyScale.y);
            LocalPosition = clickLocalPosition / lossyScale;
            UpdateObjectBounds();
            clickLocalPosition += objectBoundsSize / 2f;
            var ppi = new Vector2(
                PaintMaterial.SourceTexture.width / objectBoundsSize.x / lossyScale.x,
                PaintMaterial.SourceTexture.height / objectBoundsSize.y / lossyScale.y);
            PaintPosition = new Vector2(
                clickLocalPosition.x * lossyScale.x * ppi.x,
                clickLocalPosition.y * lossyScale.y * ppi.y);
            if (usePostPaint)
            {
                OnPostPaint();
            }
            else
            {
                UpdateBrushPreview();
            }
        }
    }
}