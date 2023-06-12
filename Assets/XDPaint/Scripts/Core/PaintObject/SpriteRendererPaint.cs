using UnityEngine;
using XDPaint.Core.PaintObject.Base;
using XDPaint.Tools.Raycast;

namespace XDPaint.Core.PaintObject
{
    public sealed class SpriteRendererPaint : BasePaintObject
    {
        private SpriteRenderer renderer;
        private Sprite sprite;
        private Plane plane;
        private Triangle triangle;
        private Ray ray;
        private Vector3 objectPosition;
        private Vector3 objectForward;
        private Vector3 objectBoundsSize;

        protected override void Init()
        {
            ObjectTransform.TryGetComponent(out renderer);
            sprite = renderer.sprite;
            UpdateObjectBounds();
        }
        
        private void InitTriangle()
        {
            var previousRotation = renderer.transform.rotation;
            renderer.transform.rotation = Quaternion.identity;
            var boundsSize = renderer.bounds.size;
            objectBoundsSize = boundsSize;
            //bottom left
            var position0 = new Vector3(-boundsSize.x / 2f, -boundsSize.y / 2f, 0);
            var uv0 = Vector2.zero;
            //upper left
            var position1 = new Vector3(-boundsSize.x / 2f, boundsSize.y / 2f, 0);
            var uv1 = Vector2.up;
            //upper right
            var position2 = new Vector3(boundsSize.x / 2f, boundsSize.y / 2f, 0);
            var uv2 = Vector2.one;
            triangle = new Triangle(position0, position1, position2, uv0, uv1, uv2);
            renderer.transform.rotation = previousRotation;
        }

        protected override bool IsInBounds(Vector3 position)
        {
            var bounds = new Bounds();
            if (renderer != null)
            {
                bounds = renderer.bounds;
            }
            ray = Camera.ScreenPointToRay(position);
            var inBounds = bounds.IntersectRay(ray);
            return inBounds;
        }

        private void UpdateObjectBounds()
        {
            if (renderer != null && objectBoundsSize != renderer.bounds.size)
            {
                InitTriangle();
            }
        }

        protected override void CalculatePaintPosition(Vector3 position, Vector2? uv = null, bool usePostPaint = true)
        {
            InBounds = IsInBounds(position);
            if (InBounds)
            {
                IsPaintingDone = true;
            }

            if (objectPosition != ObjectTransform.position || objectForward != ObjectTransform.forward)
            {
                plane = new Plane(ObjectTransform.forward, ObjectTransform.position);
                objectPosition = ObjectTransform.position;
                objectForward = ObjectTransform.forward;
            }
            
            if (plane.Raycast(ray, out var enter))
            {
                var point = ray.GetPoint(enter);
                LocalPosition = ObjectTransform.InverseTransformPoint(point);
                UpdateObjectBounds();
                var uvCoords = triangle.GetUV(LocalPosition.Value);
                PaintPosition = new Vector2(
                    Mathf.Lerp(sprite.rect.x, sprite.rect.x + sprite.rect.width, uvCoords.x),
                    Mathf.Lerp(sprite.rect.y, sprite.rect.y + sprite.rect.height, uvCoords.y));
            }

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