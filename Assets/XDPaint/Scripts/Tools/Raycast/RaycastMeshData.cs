using System.Collections.Generic;
using UnityEngine;
using XDPaint.Utils;

namespace XDPaint.Tools.Raycast
{	
	public class RaycastMeshData
	{
		public Transform Transform;
		public Mesh Mesh => mesh;
		public Vector3[] Vertices;
		public Vector2[] UV;

		public bool UseLossyScale => skinnedMeshRenderer != null;

		private SkinnedMeshRenderer skinnedMeshRenderer;
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;
		private Mesh mesh;
		private Triangle[] triangles;
		private int uvChannelIndex;

		public void Init(Component paintComponent, Component rendererComponent, int uvChannel, Triangle[] trianglesData)
		{
			mesh = new Mesh();
			uvChannelIndex = uvChannel;
			triangles = trianglesData;
			Transform = paintComponent.transform;
			skinnedMeshRenderer = rendererComponent as SkinnedMeshRenderer;
			meshRenderer = rendererComponent as MeshRenderer;
			meshFilter = paintComponent as MeshFilter;
			if (meshFilter != null)
			{
				var sharedMesh = meshFilter.sharedMesh;
				Vertices = sharedMesh.vertices;
				var uvData = new List<Vector2>();
				sharedMesh.GetUVs(uvChannelIndex, uvData);
				UV = uvData.ToArray();
			}
		}

		public bool IsRendererEquals(Component rendererComponent)
		{
			return skinnedMeshRenderer != null && skinnedMeshRenderer == rendererComponent as SkinnedMeshRenderer || 
			       meshRenderer != null && meshRenderer == rendererComponent as MeshRenderer;
		}

		public void Destroy()
		{
			if (mesh != null)
			{
				Object.Destroy(mesh);
			}
		}

		public IEnumerable<Triangle> GetNeighborsRaycasts(Triangle currentTriangle, Ray ray)
		{
			var intersects = new List<Triangle>();
			foreach (var triangleId in currentTriangle.N)
			{
				var triangle = triangles[triangleId];
				var isIntersected = IsIntersected(triangle, ray, false);
				if (isIntersected)
				{
					intersects.Add(triangle);
				}
			}
			return intersects;
		}

		public IEnumerable<Triangle> GetRaycasts(Ray ray, bool useWorld = true)
		{
			if (useWorld)
			{
				bool boundsIntersect;
				if (skinnedMeshRenderer != null)
				{
					skinnedMeshRenderer.BakeMesh(mesh);
					Vertices = mesh.vertices;
					UV = mesh.uv;
					boundsIntersect = skinnedMeshRenderer.bounds.IntersectRay(ray);
				}
				else
				{
					boundsIntersect = meshRenderer.bounds.IntersectRay(ray);
				}

				if (!boundsIntersect)
				{
					return null;
				}
				
				var origin = Transform.InverseTransformPoint(ray.origin);
				var direction = Transform.InverseTransformVector(ray.direction);
				ray = new Ray(origin, direction);
			}
			
			var intersects = new List<Triangle>();
			foreach (var triangle in triangles)
			{
				var isIntersected = IsIntersected(triangle, ray, useWorld);
				if (isIntersected)
				{
					intersects.Add(triangle);
				}
			}
			return intersects;
		}

		private bool IsIntersected(Triangle triangle, Ray ray, bool writeHit = true)
		{
			var eps = Mathf.Epsilon;
			var p1 = triangle.Position0;
			var p2 = triangle.Position1;
			var p3 = triangle.Position2;
			var e1 = p2 - p1;
			var e2 = p3 - p1;
			var p = Vector3.Cross(ray.direction, e2);
			var det = Vector3.Dot(e1, p);
			if (det.IsNaNOrInfinity() || det > eps && det < -eps)
			{
				return false;
			}
			var invDet = 1.0f / det;
			var t = ray.origin - p1;
			var u = Vector3.Dot(t, p) * invDet;
			if (u.IsNaNOrInfinity() || u < 0f || u > 1f)
			{
				return false;
			}
			var q = Vector3.Cross(t, e1);
			var v = Vector3.Dot(ray.direction, q) * invDet;
			if (v.IsNaNOrInfinity() || v < 0f || u + v > 1f)
			{
				return false;
			}
			if ((Vector3.Dot(e2, q) * invDet) > eps)
			{
				var hit = p1 + u * e1 + v * e2;
				if (writeHit)
				{
					triangle.Hit = hit;
				}
				triangle.UVHit = triangle.UV0 + (triangle.UV1 - triangle.UV0) * u + (triangle.UV2 - triangle.UV0) * v;
				return true;
			}
			return false;
		}
	}
}