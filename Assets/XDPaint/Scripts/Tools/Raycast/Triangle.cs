using System;
using System.Collections.Generic;
using UnityEngine;

namespace XDPaint.Tools.Raycast
{
	[Serializable]
	public class Triangle
	{
		//Triangle id
		public ushort Id;
		//Index 0
		public ushort I0;
		//Index 1
		public ushort I1;
		//Index 2
		public ushort I2;
		//Neighbors
		public List<ushort> N = new List<ushort>();
		
		private RaycastMeshData meshData;
		private Barycentric barycentricLocal;

		public Transform Transform => meshData.Transform;

		public Vector3 Position0
		{
			get
			{
				if (meshData.UseLossyScale)
				{
					return Vector3.Scale(meshData.Vertices[I0], Transform.lossyScale);
				}
				return meshData.Vertices[I0];
			}
		}

		public Vector3 Position1
		{
			get
			{
				if (meshData.UseLossyScale)
				{
					return Vector3.Scale(meshData.Vertices[I1], Transform.lossyScale);
				}
				return meshData.Vertices[I1];
			}
		}
		
		public Vector3 Position2
		{
			get
			{
				if (meshData.UseLossyScale)
				{
					return Vector3.Scale(meshData.Vertices[I2], Transform.lossyScale);
				}
				return meshData.Vertices[I2];
			}
		}
		
		public Vector3 Hit
		{
			get
			{
				if (barycentricLocal == null)
				{
					barycentricLocal = new Barycentric();
				}
				return barycentricLocal.Interpolate(Position0, Position1, Position2);
			}
			set => barycentricLocal = new Barycentric(Position0, Position1, Position2, value);
		}

		public Vector3 WorldHit
		{
			get
			{
				var localHit = Hit;
				return Transform.localToWorldMatrix.MultiplyPoint(localHit);
			}
		}

		private Vector2 uvHit;
		public Vector2 UVHit
		{
			get => uvHit;
			set => uvHit = value;
		}

		public Vector2 UV0 => meshData.UV[I0];
		public Vector2 UV1 => meshData.UV[I1];
		public Vector2 UV2 => meshData.UV[I2];

		public Triangle(ushort id, ushort index0, ushort index1, ushort index2)
		{
			Id = id;
			I0 = index0;
			I1 = index1;
			I2 = index2;
		}
		
		public Triangle(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
		{
			meshData = new RaycastMeshData
			{
				Vertices = new []{ vertex0, vertex1, vertex2 },
				UV = new []{ uv0, uv1, uv2 }
			};
			I0 = 0;
			I1 = 1;
			I2 = 2;
		}

		public void SetTrianglesContainer(RaycastMeshData container)
		{
			meshData = container;
		}
		
		public Vector2 GetUV(Vector3 point)
		{
			var v0 = meshData.Vertices[I0];
			var v1 = meshData.Vertices[I1];
			var v2 = meshData.Vertices[I2];

			var distance0 = v0 - point;
			var distance1 = v1 - point;
			var distance2 = v2 - point;
			//calculate the areas
			var va = Vector3.Cross(v0 - v1, v0 - v2);
			var va1 = Vector3.Cross(distance1, distance2);
			var va2 = Vector3.Cross(distance2, distance0);
			var va3 = Vector3.Cross(distance0, distance1);
			var area = va.magnitude;
			//calculate barycentric with sign
			var a1 = va1.magnitude / area * Mathf.Sign(Vector3.Dot(va, va1));
			var a2 = va2.magnitude / area * Mathf.Sign(Vector3.Dot(va, va2));
			var a3 = va3.magnitude / area * Mathf.Sign(Vector3.Dot(va, va3));
			var uv = UV0 * a1 + UV1 * a2 + UV2 * a3;
			return uv;
		}
	}
}