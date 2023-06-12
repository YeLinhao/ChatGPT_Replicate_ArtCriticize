using System.Collections.Generic;
using UnityEngine;
using XDPaint.Tools.Raycast;
using XDPaint.Utils;

namespace XDPaint.Controllers
{
	public class RaycastController : Singleton<RaycastController>
	{
		public Camera Camera { private get; set; }
		private List<RaycastMeshData> meshesData = new List<RaycastMeshData>();
		
		public void InitObject(Camera newCamera, Component paintComponent, Component renderComponent, int uvChannel, Triangle[] triangles)
		{
			Camera = newCamera;
			DestroyMeshData(renderComponent);
			var raycastMeshData = new RaycastMeshData();
			raycastMeshData.Init(paintComponent, renderComponent, uvChannel, triangles);
			if (triangles != null)
			{
				foreach (var triangle in triangles)
				{
					triangle.SetTrianglesContainer(raycastMeshData);
				}
			}
			meshesData.Add(raycastMeshData);
		}

		public Mesh GetMesh(Transform objectForPaintTransform)
		{
			return meshesData.Find(x => x.Transform == objectForPaintTransform).Mesh;
		}

		public void DestroyMeshData(Component renderComponent)
		{
			if (meshesData.Count > 0)
			{
				for (var i = meshesData.Count - 1; i >= 0; i--)
				{
					if (meshesData[i].IsRendererEquals(renderComponent))
					{
						meshesData[i].Destroy();
						meshesData.RemoveAt(i);
						break;
					}
				}
			}
		}
		
		public void Raycast(Ray ray, out Triangle triangle)
		{
			var triangles = new List<Triangle>();
			foreach (var meshData in meshesData)
			{
				if (meshData == null || meshData.Transform == null || !meshData.Transform.gameObject.activeInHierarchy)
					continue;

				var raycastResult = meshData.GetRaycasts(ray);
				if (raycastResult != null)
				{
					triangles.AddRange(raycastResult);
				}
			}
			triangle = SortIntersects(triangles);
		}
		
		public void RaycastLocal(Ray ray, Transform objectTransform, out Triangle triangle)
		{
			var triangles = new List<Triangle>();
			foreach (var meshData in meshesData)
			{
				if (meshData == null || meshData.Transform == null || !meshData.Transform.gameObject.activeInHierarchy)
					continue;

				if (objectTransform == meshData.Transform)
				{
					var raycastResult = meshData.GetRaycasts(ray, false);
					if (raycastResult != null)
					{
						triangles.AddRange(raycastResult);
					}
				}
			}
			triangle = SortIntersects(triangles);
		}

		public void NeighborsRaycast(Triangle triangle, Ray ray, out Triangle outTriangle)
		{
			var triangles = new List<Triangle>();
			foreach (var meshData in meshesData)
			{
				if (triangle.Transform == meshData.Transform)
				{
					var raycastResult = meshData.GetNeighborsRaycasts(triangle, ray);
					if (raycastResult != null)
					{
						triangles.AddRange(raycastResult);
					}
				}
			}
			outTriangle = SortIntersects(triangles);
		}
		
		private Triangle SortIntersects(IList<Triangle> triangles)
		{
			if (triangles.Count == 0)
			{
				return null;
			}
			if (triangles.Count == 1)
			{
				return triangles[0];
			}
			var result = triangles[0];
			var cameraPosition = Camera.transform.position;
			var currentDistance = Vector3.Distance(cameraPosition, result.WorldHit);
			for (var i = 1; i < triangles.Count; i++)
			{
				var distance = Vector3.Distance(cameraPosition, triangles[i].WorldHit);
				if (distance < currentDistance)
				{
					currentDistance = distance;
					result = triangles[i];
				}
			}
			return result;
		}
	}
}