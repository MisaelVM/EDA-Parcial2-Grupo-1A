using System;
using UnityEngine;

namespace SVO
{
	public static class OctreeMath
	{
		public static Vector3 ToBarycentricCoordinates(Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			var norm = Vector3.Cross(p2 - p1, p3 - p1);
			var pointProj = point - (Vector3.Dot(point - p1, norm) / norm.sqrMagnitude * norm);
			
			
			var a1 = CalculateTriangleArea(pointProj, p2, p3);
			var a2 = CalculateTriangleArea(p1, pointProj, p3);
			var a3 = CalculateTriangleArea(p1, p2, pointProj);
			return new Vector3(a1, a2, a3) / (a1 + a2 + a3);
		}

		public static float CalculateTriangleArea(Vector3 p1, Vector3 p2, Vector3 p3)
		{
			return Vector3.Cross(p2 - p1, p3 - p1).magnitude / 2;
		}

		public static float EstimateDerivative(float x, float dx, Func<float, float> f) 
			=> (f(x + dx) - f(x)) / dx;
	}
}