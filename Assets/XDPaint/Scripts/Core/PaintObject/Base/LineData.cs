using System.Collections.Generic;
using UnityEngine;
using XDPaint.Tools.Raycast;

namespace XDPaint.Core.PaintObject.Base
{
	public class LineData
	{
		private List<Triangle> triangles = new List<Triangle>();
		private List<Vector2> paintPositions = new List<Vector2>();
		private List<float> brushSizes = new List<float>();

		public void AddBrush(float brushSize)
		{
			if (brushSizes.Count > 1)
			{
				brushSizes.RemoveAt(0);
			}
			brushSizes.Add(brushSize);
		}

		public void AddPosition(Vector2 position)
		{
			if (paintPositions.Count > 1)
			{
				paintPositions.RemoveAt(0);
			}
			paintPositions.Add(position);
		}

		public void AddTriangleBrush(Triangle triangle, float brushSize)
		{
			if (triangles.Count > 1)
			{
				triangles.RemoveAt(0);
			}
			triangles.Add(triangle);
			if (brushSizes.Count > 1)
			{
				brushSizes.RemoveAt(0);
			}
			brushSizes.Add(brushSize);
		}

		public float[] GetBrushes()
		{
			return brushSizes.ToArray();
		}
		
		public Triangle[] GetTriangles()
		{
			return triangles.ToArray();
		}
		
		public Vector2[] GetPositions()
		{
			return paintPositions.ToArray();
		}

		public bool HasOnePosition()
		{
			return paintPositions.Count == 1;
		}

		public bool HasNotSameTriangles()
		{
			return triangles.Count == 2 && triangles[0].Id != triangles[1].Id;
		}

		public void Clear()
		{
			triangles.Clear();
			paintPositions.Clear();
			brushSizes.Clear();
		}
	}
}