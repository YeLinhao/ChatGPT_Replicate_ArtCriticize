using UnityEngine;
using XDPaint.Core.PaintObject.Base;

namespace XDPaint.Core.PaintObject
{
	public sealed class MeshRendererPaint : BasePaintObject
	{
		private Renderer renderer;

		protected override void Init()
		{
			ObjectTransform.TryGetComponent(out renderer);

			Mesh mesh = null;
			if (ObjectTransform.TryGetComponent<MeshFilter>(out var meshFilter))
			{
				mesh = meshFilter.sharedMesh;
			}
			else
			{
				if (ObjectTransform.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
				{
					mesh = skinnedMeshRenderer.sharedMesh;
				}
			}
			if (mesh == null)
			{
				Debug.LogError("Can't find MeshFilter or SkinnedMeshRenderer component!");
			}
			if (Camera.orthographic)
			{
				Debug.LogWarning("Camera is not perspective!");
			}
		}

		protected override bool IsInBounds(Vector3 position)
		{
			var bounds = new Bounds();
			if (renderer != null)
			{
				bounds = renderer.bounds;
			}
			var ray = Camera.ScreenPointToRay(position);
			var inBounds = bounds.IntersectRay(ray);
			return inBounds;
		}
		
		protected override void CalculatePaintPosition(Vector3 position, Vector2? uv = null, bool usePostPaint = true)
		{
			InBounds = IsInBounds(position);
			if (!InBounds)
			{
				PaintPosition = null;
				if (usePostPaint)
				{
					OnPostPaint();
				}
				else
				{
					UpdateBrushPreview();
				}
				return;
			}

			var hasRaycast = uv != null;
			if (hasRaycast)
			{
				PaintPosition = new Vector2(PaintMaterial.SourceTexture.width * uv.Value.x, PaintMaterial.SourceTexture.height * uv.Value.y);
				IsPaintingDone = true;
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