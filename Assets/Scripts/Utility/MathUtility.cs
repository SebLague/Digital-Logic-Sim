using UnityEngine;

public static class MathUtility {

	public static bool LineSegmentsIntersect (Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
		float d = (b2.x - b1.x) * (a1.y - a2.y) - (a1.x - a2.x) * (b2.y - b1.y);
		if (d == 0)
			return false;
		float t = ((b1.y - b2.y) * (a1.x - b1.x) + (b2.x - b1.x) * (a1.y - b1.y)) / d;
		float u = ((a1.y - a2.y) * (a1.x - b1.x) + (a2.x - a1.x) * (a1.y - b1.y)) / d;

		return t >= 0 && t <= 1 && u >= 0 && u <= 1;
	}

	public static bool LinesIntersect (Vector2 a1, Vector2 a2, Vector2 a3, Vector2 a4) {
		return (a1.x - a2.x) * (a3.y - a4.y) - (a1.y - a2.y) * (a3.x - a4.x) != 0;
	}

	public static Vector2 PointOfLineLineIntersection (Vector2 a1, Vector2 a2, Vector2 a3, Vector2 a4) {
		float d = (a1.x - a2.x) * (a3.y - a4.y) - (a1.y - a2.y) * (a3.x - a4.x);
		if (d == 0) {
			Debug.LogError ("Lines are parallel, please check that this is not the case before calling line intersection method");
			return Vector2.zero;
		} else {
			float n = (a1.x - a3.x) * (a3.y - a4.y) - (a1.y - a3.y) * (a3.x - a4.x);
			float t = n / d;
			return a1 + (a2 - a1) * t;
		}
	}

	/// Distance of point P from the line passing through points A and B.
	public static float DistanceFromPointToLine (Vector2 a, Vector2 b, Vector2 p) {
		float s1 = -b.y + a.y;
		float s2 = b.x - a.x;
		return Mathf.Abs ((p.x - a.x) * s1 + (p.y - a.y) * s2) / Mathf.Sqrt (s1 * s1 + s2 * s2);
	}

	/// Psuedo distance of point C from the line passing through points A and B.
	/// This is not the actual distance value, but a further point will always have a higher value than a nearer point.
	/// Faster than calculating the actual distance. Useful for sorting.
	public static float PseudoDistanceFromPointToLine (Vector2 a, Vector2 b, Vector2 p) {
		return Mathf.Abs ((p.x - a.x) * (-b.y + a.y) + (p.y - a.y) * (b.x - a.x));
	}

	public static Vector2 ClosestPointOnLineSegment (Vector2 a, Vector2 b, Vector2 p) {
		Vector2 aB = b - a;
		Vector2 aP = p - a;
		float sqrLenAB = aB.sqrMagnitude;

		if (sqrLenAB == 0)
			return a;

		float t = Mathf.Clamp01 (Vector2.Dot (aP, aB) / sqrLenAB);
		return a + aB * t;
	}

	public static Vector3 ClosestPointOnLineSegment (Vector3 a, Vector3 b, Vector3 p) {
		Vector3 aB = b - a;
		Vector3 aP = p - a;
		float sqrLenAB = aB.sqrMagnitude;

		if (sqrLenAB == 0)
			return a;

		float t = Mathf.Clamp01 (Vector3.Dot (aP, aB) / sqrLenAB);
		return a + aB * t;
	}

	public static int SideOfLine (Vector2 a, Vector2 b, Vector2 p) {
		return (int) Mathf.Sign ((p.x - a.x) * (-b.y + a.y) + (p.y - a.y) * (b.x - a.x));
	}

	/// returns the smallest angle between ABC. Never greater than 180
	public static float MinAngle (Vector3 a, Vector3 b, Vector3 c) {
		return Vector3.Angle ((a - b), (c - b));
	}

	public static bool PointInTriangle (Vector2 a, Vector2 b, Vector2 c, Vector2 p) {
		float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
		float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
		float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
		return s >= 0 && t >= 0 && (s + t) <= 1;
	}

	public static bool PointsAreClockwise (Vector2[] points) {
		float signedArea = 0;
		for (int i = 0; i < points.Length; i++) {
			int nextIndex = (i + 1) % points.Length;
			signedArea += (points[nextIndex].x - points[i].x) * (points[nextIndex].y + points[i].y);
		}

		return signedArea >= 0;
	}

	public static bool RaySphere (Vector3 centre, float radius, Vector3 rayOrigin, Vector3 rayDir, out Vector3 intersectionPoint) {
		// See: http://viclw17.github.io/2018/07/16/raytracing-ray-sphere-intersection/
		Vector3 offset = rayOrigin - centre;
		float a = Vector3.Dot (rayDir, rayDir); // sqr ray length (in case of non-normalized rayDir)
		float b = 2 * Vector3.Dot (offset, rayDir);
		float c = Vector3.Dot (offset, offset) - radius * radius;
		float discriminant = b * b - 4 * a * c;

		// No intersections: discriminant < 0
		// 1 intersection: discriminant == 0
		// 2 intersections: discriminant > 0
		if (discriminant >= 0) {
			float t = (-b - Mathf.Sqrt (discriminant)) / (2 * a);
			//float t2 = (-b + Mathf.Sqrt (discriminant)) / (2 * a); // The further away intersection point

			// If t is negative, the intersection was in negative ray direction so ignore it
			if (t >= 0) {
				intersectionPoint = rayOrigin + rayDir * t;
				return true;
			}
		}

		intersectionPoint = Vector3.zero;
		return false;
	}

	public static Vector2 V3ToXZ (Vector3 v3) {
		return new Vector2 (v3.x, v3.z);
	}

	public static Vector3 V2ToXYZ (Vector2 v2, float yValue) {
		return new Vector3 (v2.x, yValue, v2.y);
	}

}