using System;
using System.Collections.Generic;
using UnityEngine;
using XDPaint.Tools.Raycast;

namespace XDPaint.Tools.Triangles
{
    public static class TrianglesData
    {
        public static Action<float> OnUpdate;
        public static Action OnFinish;
        
        private static bool shouldBreak;
        public static void Break()
        {
            shouldBreak = true;
        }
        
        public static Triangle[] GetData(Mesh mesh, int subMeshIndex, int uvChannel, bool fillNeighbors = true)
        {
            var indices = mesh.GetTriangles(subMeshIndex);
            if (indices.Length == 0)
            {
                Debug.LogError("Mesh doesn't have indices!");
                return Array.Empty<Triangle>();
            }
            var uvData = new List<Vector2>();
            mesh.GetUVs(uvChannel, uvData);
            if (uvData.Count == 0)
            {
                Debug.LogError("Mesh doesn't have UV in the selected channel!");
                return Array.Empty<Triangle>();
            }

            var indexesCount = indices.Length;
            var triangles = new Triangle[indexesCount / 3];
            for (var i = 0; i < indexesCount; i += 3)
            {
                var index = i / 3;
                var index0 = indices[i + 0];
                var index1 = indices[i + 1];
                var index2 = indices[i + 2];
                triangles[index] = new Triangle((ushort)index, (ushort)index0, (ushort)index1, (ushort)index2);
            }

            if (fillNeighbors)
            {
                for (var i = 0; i < triangles.Length; i++)
                {
                    if (OnUpdate != null)
                    {
                        OnUpdate(i / (float)triangles.Length);
                    }
                    if (shouldBreak)
                        break;
                    
                    var triangle = triangles[i];
                    var index0 = triangle.I0;
                    var index1 = triangle.I1;
                    var index2 = triangle.I2;

                    foreach (var triangleFind in triangles)
                    {
                        var indexFind0 = triangleFind.I0;
                        var indexFind1 = triangleFind.I1;
                        var indexFind2 = triangleFind.I2;

                        if (triangleFind.Id != triangle.Id)
                        {
                            if (index0 == indexFind0 || index0 == indexFind1 || index0 == indexFind2 ||
                                index1 == indexFind0 || index1 == indexFind1 || index1 == indexFind2 ||
                                index2 == indexFind0 || index2 == indexFind1 || index2 == indexFind2)
                            {
                                if (!triangle.N.Contains(triangleFind.Id))
                                {
                                    triangle.N.Add(triangleFind.Id);
                                }
                            }
                        }
                    }
                }
                shouldBreak = false;
                if (OnFinish != null)
                {
                    OnFinish();
                }
            }
            return triangles;
        }
    }
}