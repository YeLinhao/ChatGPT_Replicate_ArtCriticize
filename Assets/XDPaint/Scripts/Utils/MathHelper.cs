using System;
using System.Collections.Generic;
using UnityEngine;
using XDPaint.Tools.Raycast;

namespace XDPaint.Utils
{
    public static class MathHelper
    {
        public static Vector2 GetIntersectionUV(Triangle triangle, Ray ray)
        {
            var p1 = triangle.Position0;
            var p2 = triangle.Position1;
            var p3 = triangle.Position2;
            var e1 = p2 - p1;
            var e2 = p3 - p1;
            var p = Vector3.Cross(ray.direction, e2);
            var det = Vector3.Dot(e1, p);
            var invDet = 1.0f / det;
            var t = ray.origin - p1;
            var u = Vector3.Dot(t, p) * invDet;
            var q = Vector3.Cross(t, e1);
            var v = Vector3.Dot(ray.direction, q) * invDet;
            var result = triangle.UV0 + (triangle.UV1 - triangle.UV0) * u + (triangle.UV2 - triangle.UV0) * v;
            return result;
        }
        
        public static Vector3 GetExitPointFromTriangle(Camera camera, Triangle triangle, Vector3 firstHit, Vector3 lastHit, Vector3 normal)
        {
            Vector3 intersectionEdge1, intersectionEdge2, intersectionEdge3 = Vector3.zero;
            var isIntersectedEdge3 = false;
            var isIntersectedEdge1 = IsPlaneIntersectLine(normal, firstHit, triangle.Position0, triangle.Position1, out intersectionEdge1);
            var isIntersectedEdge2 = IsPlaneIntersectLine(normal, firstHit, triangle.Position1, triangle.Position2, out intersectionEdge2);
            if (!isIntersectedEdge1 || !isIntersectedEdge2)
            {
                isIntersectedEdge3 = IsPlaneIntersectLine(normal, firstHit, triangle.Position0, triangle.Position2, out intersectionEdge3);
            }

            var intersectionsCount = isIntersectedEdge1 ? 1 : 0;
            intersectionsCount += isIntersectedEdge2 ? 1 : 0;
            intersectionsCount += isIntersectedEdge3 ? 1 : 0;
            if (intersectionsCount == 0)
            {
                Debug.LogWarning("Can't find intersection. Zero intersections");
                return firstHit;
            }

            var filled = 0;
            var intersectionsEdges = new KeyValuePair<int, Vector3>[intersectionsCount];
            if (isIntersectedEdge1)
            {
                intersectionsEdges[filled] = new KeyValuePair<int, Vector3>(filled, intersectionEdge1);
                filled++;
            }
            if (isIntersectedEdge2)
            {
                intersectionsEdges[filled] = new KeyValuePair<int, Vector3>(filled, intersectionEdge2);
                filled++;
            }
            if (isIntersectedEdge3)
            {
                intersectionsEdges[filled] = new KeyValuePair<int, Vector3>(filled, intersectionEdge3);
            }

            var indexEdge = 0;
            if (intersectionsCount == 1)
            {
                indexEdge = intersectionsEdges[0].Key;
            }
            else if (intersectionsCount == 2)
            {
                var p0 = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(intersectionsEdges[0].Value));
                var p1 = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(intersectionsEdges[1].Value));
                var pEnd = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(lastHit));
                var distFirst = Vector3.Distance(p0, pEnd);
                var distLast = Vector3.Distance(p1, pEnd);

                indexEdge = distFirst < distLast ? intersectionsEdges[0].Key : intersectionsEdges[1].Key;
            }
            else
            {
                var p0 = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(intersectionsEdges[0].Value));
                var p1 = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(intersectionsEdges[1].Value));
                var p2 = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(intersectionsEdges[2].Value));
                var pEnd = camera.WorldToScreenPoint(triangle.Transform.TransformPoint(lastHit));
                var dist1 = Vector3.Distance(p0, pEnd);
                var dist2 = Vector3.Distance(p1, pEnd);
                var dist3 = Vector3.Distance(p2, pEnd);
                Vector3 resultVector;

                if (dist1 < dist2)
                {
                    indexEdge = intersectionsEdges[0].Key;
                    resultVector = p0;
                }
                else
                {
                    indexEdge = intersectionsEdges[1].Key;
                    resultVector = p1;
                }
                if (Vector3.Distance(resultVector, pEnd) > dist3)
                {
                    indexEdge = intersectionsEdges[2].Key;
                }
            }

            return intersectionsEdges[indexEdge].Value;
        }

        private static bool IsPlaneIntersectLine(Vector3 n, Vector3 a, Vector3 w, Vector3 v, out Vector3 p)
        {
            p = Vector3.zero;
            var dotProduct = Vector3.Dot(n, (w - v));
            if (Math.Abs(dotProduct) < Mathf.Epsilon)
            {
                return false;
            }
            var dot1 = Vector3.Dot(n, (a - v));
            var t = dot1 / dotProduct;
            if (t > 1f || t < 0f)
            {
                return false;
            }
            p = v + t * (w - v);
            return true;
        }
    }
}