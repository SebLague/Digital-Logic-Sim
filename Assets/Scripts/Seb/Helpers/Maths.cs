using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static System.Math;
using static System.MathF;
using Random = System.Random;

namespace Seb.Helpers
{
	public static class Maths
	{
		#region #Structures

		public struct RaySphereResult
		{
			public bool intersects;
			public float dstToSphere;
			public float dstThroughSphere;
		}

		#endregion

		/* OVERVIEW:
		 * #Constants
		 * #Easing
		 * #ShapesAndLines2D
		 * #Intersection-Distance-Contains
		 * #RNGUtils
		 * #StatsAndCounting
		 * #Miscellaneous
		 * #SphericalGeometry
		 */

		#region #Constants

		public const float PI = 3.1415926f;
		public const float TAU = 2 * PI;
		public const float Epsilon = 1.175494351E-38f;

		#endregion

		#region #Easing

		public static float EaseQuadIn(float t) => Square(Clamp01(t));
		public static float EaseQuadOut(float t) => 1 - Square(1 - Clamp01(t));
		public static float EaseQuadInOut(float t) => 3 * Square(Clamp01(t)) - 2 * Cube(Clamp01(t));

		public static float EaseCubeIn(float t) => Cube(Clamp01(t));
		public static float EaseCubeOut(float t) => 1 - Cube(1 - Clamp01(t));

		public static float EaseCubeInOut(float t)
		{
			t = Clamp01(t);
			int r = (int)Math.Round(t);
			return 4 * Cube(t) * (1 - r) + (1 - 4 * Cube(1 - t)) * r;
		}

		#endregion

		#region #ShapesAndLines2D

		/// <summary> Returns the area of a triangle in 3D space. </summary>
		public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
		{
			// Thanks to https://math.stackexchange.com/a/1951650
			Vector3 ortho = Vector3.Cross(c - a, b - a);
			float parallogramArea = ortho.magnitude;
			return parallogramArea * 0.5f;
		}

		public static bool TriangleIsClockwiseTest(Vector2 a, Vector2 b, Vector2 c) => Vector2.Dot(b - a, Perpendicular(c - b)) > 0;

		public static bool PolygonIsClockwiseTest(Vector2[] points)
		{
			float extremeX = float.MinValue;
			float extremeY = float.MinValue;
			int extremeIndex = -1;

			for (int i = 0; i < points.Length; i++)
			{
				Vector2 p = points[i];
				if (p.x > extremeX || (p.x == extremeX && p.y > extremeY))
				{
					extremeX = p.x;
					extremeY = p.y;
					extremeIndex = i;
				}
			}

			Vector2 a = points[extremeIndex];
			Vector2 b = points[(extremeIndex + 1) % points.Length];
			Vector2 c = points[(extremeIndex - 1 + points.Length) % points.Length];

			return TriangleIsClockwiseTest(a, b, c);
		}

		public static Vector2 Perpendicular(Vector2 v) => new(-v.y, v.x);


		/// <summary>
		///     Returns the signed area of a polygon (negative if clockwise)
		///     Note: does not matter whether endpoints are duplicate
		/// </summary>
		public static float PolygonAreaSigned(Vector2[] points)
		{
			// Ignore last point if it is a duplicate of the first
			int numPoints = (points[^1] - points[0]).sqrMagnitude < 0.00001f ? points.Length - 1 : points.Length;

			float area = 0;
			for (int i = 0; i < numPoints; i++)
			{
				Vector2 a = points[i];
				Vector2 b = points[(i + 1) % points.Length];
				// We can calculate the polygon area by summing signed areas of triangles between each edge and a fixed point (here
				// chosen as the origin to simplify calculations). This is the same idea as rendering glyphs with even-odd rule, since
				// the sign of the triangle area cancels out overlapping parts when coming around the other side of the polygon.
				// Note also that the factor of 0.5 in triangle area is deferred to the end. 
				area += (a.x + b.x) * (b.y - a.y);
			}

			return area * 0.5f;
		}


		/// <summary>
		///     Returns the centroid (centre of mass) of a polygon
		///     Note: order of points does not matter, nor whether endpoints are duplicate
		/// </summary>
		public static Vector2 PolygonCentreOfMass(Vector2[] points)
		{
			// Ignore last point if it is a duplicate of the first
			int numPoints = (points[^1] - points[0]).sqrMagnitude < 0.00001f ? points.Length - 1 : points.Length;

			float xSum = 0;
			float ySum = 0;
			float area = 0;

			for (int i = 0; i < numPoints; i++)
			{
				Vector2 a = points[i];
				Vector2 b = points[(i + 1) % points.Length];
				area += (a.x + b.x) * (b.y - a.y) / 2;

				float x = (a.x + b.x) * (a.x * b.y - b.x * a.y);
				float y = (a.y + b.y) * (a.x * b.y - b.x * a.y);
				xSum += x;
				ySum += y;
			}

			return new Vector2(xSum, ySum) / (6 * area);
		}

		/// <summary>
		///     Returns the signed area of a triangle in 2D space.
		///     The sign depends on whether the points are given in clockwise (negative) or counter-clockwise (positive) order.
		/// </summary>
		public static float TriangleAreaSigned2D(Vector2 a, Vector2 b, Vector2 c) => 0.5f * ((a.x - b.x) * (a.y - c.y) + (a.y - b.y) * (c.x - a.x));

		/*  // ---- Unoptimized version ----
		 *  // Consider AC as the base, and calculate line perpendicular to it (of same length).
		 *	// Then, take adjacent edge such as AB and project it onto that to get height * base
		 *	Vector2 baseEdge = c - a;
		 *	Vector2 basePerp = new Vector2(-baseEdge.y, baseEdge.x);
		 *	return 0.5f * Vector2.Dot(basePerp, a - b);
		 */
		public static bool PointInCircle2D(Vector2 point, Vector2 circleCentre, float circleRadius) => (point - circleCentre).sqrMagnitude <= circleRadius * circleRadius;

		/// <summary> Test if point p is inside the triangle (a, b, c) </summary>
		public static bool TriangleContainsPoint(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			// Thanks to https://stackoverflow.com/a/14382692
			float area = TriangleAreaSigned2D(a, b, c);
			float s = (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y) * Mathf.Sign(area);
			float t = (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y) * Mathf.Sign(area);
			return s >= 0 && t >= 0 && s + t < 2 * Mathf.Abs(area);
		}

		/// <summary> Determines whether the given 2D triangle is wound in a clockwise order</summary>
		public static bool TriangleIsClockwise(Vector2 a, Vector2 b, Vector2 c) => TriangleAreaSigned2D(a, b, c) < 0;

		/// <summary>
		///     Test if a 2D polygon contains the given point.
		///     Points can be ordered clockwise or counterclockwise.
		///     Note: function doesn't care if last point in polygon is duplicate of first point or not
		/// </summary>
		public static bool PolygonContainsPoint(Vector2 p, Vector2[] points)
		{
			// Thanks to Dan Sunday
			int windingNumber = 0;

			// Ignore last point if it is a duplicate of the first
			int numPoints = (points[^1] - points[0]).sqrMagnitude < 0.00001f ? points.Length - 1 : points.Length;

			for (int i = 0; i < numPoints; i++)
			{
				Vector2 a = points[i];
				Vector2 b = points[(i + 1) % points.Length];

				if (a.y <= p.y)
				{
					if (b.y > p.y && PointIsOnLeftSideOfLine(p, a, b))
					{
						windingNumber++;
					}
				}
				else if (b.y <= p.y && !PointIsOnLeftSideOfLine(p, a, b))
				{
					windingNumber--;
				}
			}

			return windingNumber != 0;

			// Calculate which side of line AB point P is on
			bool PointIsOnLeftSideOfLine(Vector2 p, Vector2 a, Vector2 b)
			{
				return (b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y) > 0;
			}
		}

		public static bool PointInBox2D(Vector2 point, Vector2 boxCentre, Vector2 boxSize)
		{
			float ox = Mathf.Abs(point.x - boxCentre.x);
			float oy = Mathf.Abs(point.y - boxCentre.y);
			return ox < boxSize.x / 2 && oy < boxSize.y / 2;
		}

		public static bool BoxesOverlap(Vector2 centreA, Vector2 sizeA, Vector2 centreB, Vector2 sizeB)
		{
			float leftA = centreA.x - sizeA.x / 2;
			float rightA = centreA.x + sizeA.x / 2;
			float topA = centreA.y + sizeA.y / 2;
			float bottomA = centreA.y - sizeA.y / 2;

			float leftB = centreB.x - sizeB.x / 2;
			float rightB = centreB.x + sizeB.x / 2;
			float topB = centreB.y + sizeB.y / 2;
			float bottomB = centreB.y - sizeB.y / 2;

			return leftA <= rightB && rightA >= leftB && topA >= bottomB && bottomA <= topB;
		}

		#endregion

		#region #LineAndRayIntersections

		public static RaySphereResult RayIntersectsSphere(Vector3 rayOrigin, Vector3 rayDir, Vector3 centre, float radius)
		{
			Vector3 offset = rayOrigin - centre;
			const float a = 1;
			float b = 2 * Vector3.Dot(offset, rayDir);
			float c = Vector3.Dot(offset, offset) - radius * radius;
			float d = b * b - 4 * c; // Discriminant from quadratic formula

			// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
			if (d > 0)
			{
				float s = Mathf.Sqrt(d);
				float dstToSphereNear = Mathf.Max(0, -b - s) / (2 * a);
				float dstToSphereFar = (-b + s) / (2 * a);

				// Ignore intersections that occur behind the ray
				if (dstToSphereFar >= 0)
				{
					return new RaySphereResult
					{
						intersects = true,
						dstToSphere = dstToSphereNear,
						dstThroughSphere = dstToSphereFar - dstToSphereNear
					};
				}
			}

			// Ray did not intersect sphere
			return new RaySphereResult
			{
				intersects = false,
				dstToSphere = Mathf.Infinity,
				dstThroughSphere = 0
			};
		}

		/// <summary> Get point on the line segment (a1, a2) that's closest to the given point (p) </summary>
		public static Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 a1, Vector3 a2)
		{
			Vector3 lineDelta = a2 - a1;
			Vector3 pointDelta = p - a1;
			float sqrLineLength = lineDelta.sqrMagnitude;

			if (sqrLineLength == 0)
				return a1;

			float t = Mathf.Clamp01(Vector3.Dot(pointDelta, lineDelta) / sqrLineLength);
			return a1 + lineDelta * t;
		}

		/// <summary> Calculates smallest distance from given point to the line segment (a1, a2)</summary>
		public static float DistanceToLineSegment(Vector3 p, Vector3 a1, Vector3 a2)
		{
			Vector3 closestPoint = ClosestPointOnLineSegment(p, a1, a2);
			return (p - closestPoint).magnitude;
		}

		/// <summary> Calculates smallest distance from given point to the line segment (a1, a2)</summary>
		public static float SqrDistanceToLineSegment(Vector3 p, Vector3 a1, Vector3 a2) => (p - ClosestPointOnLineSegment(p, a1, a2)).sqrMagnitude;

		/// <summary> Test if two infinite 2D lines intersect (true unless parallel), and get point of intersection </summary>
		public static (bool intersects, Vector2 point) LineIntersectsLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			float d = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
			// Check if parallel
			if (ApproximatelyEqual(d, 0))
			{
				return (false, Vector2.zero);
			}

			float n = (a1.x - b1.x) * (b1.y - b2.y) - (a1.y - b1.y) * (b1.x - b2.x);
			float t = n / d;
			Vector2 intersectionPoint = a1 + (a2 - a1) * t;
			return (true, intersectionPoint);
		}

		static float Determinant(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

		public static (bool intersects, Vector2 point) LineSegIntersectsLineSegTest(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			float d = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
			// Check if parallel
			if (d == 0) return (false, Vector2.zero);

			float n = (a1.x - b1.x) * (b1.y - b2.y) - (a1.y - b1.y) * (b1.x - b2.x);
			float t = n / d;
			Vector2 intersectionPoint = a1 + (a2 - a1) * t;

			bool onSegA = Vector2.Dot(a1 - intersectionPoint, a2 - intersectionPoint) <= 0;
			bool onSegB = Vector2.Dot(b1 - intersectionPoint, b2 - intersectionPoint) <= 0;
			return (onSegA && onSegB, intersectionPoint);
		}

		public static bool LineSegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			float d = (b2.x - b1.x) * (a1.y - a2.y) - (a1.x - a2.x) * (b2.y - b1.y);
			if (d == 0)
				return false;
			float t = ((b1.y - b2.y) * (a1.x - b1.x) + (b2.x - b1.x) * (a1.y - b1.y)) / d;
			float u = ((a1.y - a2.y) * (a1.x - b1.x) + (a2.x - a1.x) * (a1.y - b1.y)) / d;

			return t >= 0 && t <= 1 && u >= 0 && u <= 1;
		}

		/// <summary> Test if ray intersects line segment, and get point of intersection </summary>
		public static (bool intersects, Vector2 point) RayIntersectsLineSegment(Vector2 rayOrigin, Vector2 rayDir, Vector2 lineA, Vector2 lineB)
		{
			Vector2 ab = lineA - lineB;
			Vector2 abPerp = new(-ab.y, ab.x);
			float rayDotABPerp = Vector2.Dot(rayDir, abPerp);

			if (rayDotABPerp == 0) return (false, rayOrigin);

			float dst = Vector2.Dot(lineA - rayOrigin, abPerp) / rayDotABPerp;
			Vector2 intersectionPoint = rayOrigin + rayDir * dst;
			bool isOnSegment = Vector2.Dot(lineA - intersectionPoint, lineB - intersectionPoint) <= 0;

			return (dst >= 0 && isOnSegment, intersectionPoint);
		}


		/// <summary> Test if ray intersects infinite line, and get point of intersection </summary>
		public static (bool intersects, Vector2 point) RayIntersectsLine(Vector2 rayOrigin, Vector2 rayDir, Vector2 lineA, Vector2 lineB)
		{
			Vector2 lineOffset = lineA - lineB;
			float d = Determinant(-rayDir, lineOffset);
			// Check if parallel
			if (ApproximatelyEqual(d, 0))
			{
				return (false, Vector2.zero);
			}

			float n = Determinant(rayOrigin - lineA, lineOffset);
			float t = n / d;
			Vector2 intersectionPoint = rayOrigin + rayDir * t;
			bool intersectsInFrontOfRay = t >= 0;
			return (intersectsInFrontOfRay, intersectionPoint);
		}

		// Thanks to https://tavianator.com/2011/ray_box.html
		public static (bool hit, float dst) RayBoundingBox(Vector3 rayOrigin, Vector3 rayDir, Vector3 boxMin, Vector3 boxMax)
		{
			float invDirX = rayDir.x == 0 ? float.PositiveInfinity : 1 / rayDir.x;
			float invDirY = rayDir.y == 0 ? float.PositiveInfinity : 1 / rayDir.y;
			float invDirZ = rayDir.z == 0 ? float.PositiveInfinity : 1 / rayDir.z;

			float tx1 = (boxMin.x - rayOrigin.x) * invDirX;
			float tx2 = (boxMax.x - rayOrigin.x) * invDirX;
			float tmin = Mathf.Min(tx1, tx2);
			float tmax = Mathf.Max(tx1, tx2);

			float ty1 = (boxMin.y - rayOrigin.y) * invDirY;
			float ty2 = (boxMax.y - rayOrigin.y) * invDirY;
			tmin = Mathf.Max(tmin, Mathf.Min(ty1, ty2));
			tmax = Mathf.Min(tmax, Mathf.Max(ty1, ty2));

			float tz1 = (boxMin.z - rayOrigin.z) * invDirZ;
			float tz2 = (boxMax.z - rayOrigin.z) * invDirZ;
			tmin = Mathf.Max(tmin, Mathf.Min(tz1, tz2));
			tmax = Mathf.Min(tmax, Mathf.Max(tz1, tz2));

			bool hit = tmax >= tmin && tmax > 0;
			float dst = tmin > 0 ? tmin : tmax;
			return (hit, dst);
		}

		// Calculate the intersection of a ray with a triangle using Moller-Trumbore algorithm
		// Thanks to https://stackoverflow.com/a/42752998
		public static (bool hit, float dst) RayTriangle(Vector3 rayOrigin, Vector3 rayDir, Vector3 triA, Vector3 triB, Vector3 triC)
		{
			Vector3 edgeAB = triB - triA;
			Vector3 edgeAC = triC - triA;
			Vector3 ao = rayOrigin - triA;
			Vector3 dao = Vector3.Cross(ao, rayDir);
			Vector3 normalVector = Vector3.Cross(edgeAB, edgeAC);

			float determinant = -Vector3.Dot(rayDir, normalVector);
			float invDet = 1 / determinant;

			// Calculate dst to triangle & barycentric coordinates of intersection point
			float dst = Vector3.Dot(ao, normalVector) * invDet;
			float u = Vector3.Dot(edgeAC, dao) * invDet;
			float v = -Vector3.Dot(edgeAB, dao) * invDet;
			float w = 1 - u - v;

			// Initialize hit info
			bool hit = determinant >= 1E-6 && dst >= 0 && u >= 0 && v >= 0 && w >= 0;
			return (hit, dst);
		}


		/// <summary> Returns -1 or +1 depending which side point p is of the line (a1, a2). Returns 0 if on line. </summary>
		public static int SideOfLine(Vector2 p, Vector2 a, Vector2 b)
		{
			float det = Determinant(b - a, p - a);
			return Math.Sign(det);
		}

		/// <summary> Test if points p1 and p2 are on the same side of the line (a1, a2) </summary>
		public static bool PointOnSameSideOfLine(Vector2 p1, Vector2 p2, Vector2 a1, Vector2 a2) => SideOfLine(p1, a1, a2) == SideOfLine(p2, a1, a2);

		/// <summary>
		///     Given an infinite plane defined by some point that the plane passes through, as well as the normal vector of the plane,
		///     this function returns the nearest point on that plane to the given point p.
		/// </summary>
		public static Vector3 ClosestPointOnPlane(Vector3 anyPointOnPlane, Vector3 planeNormal, Vector3 p)
		{
			float signedDstToPlane = Vector3.Dot(anyPointOnPlane - p, planeNormal);
			Vector3 closestPointOnPlane = p + planeNormal * signedDstToPlane;
			return closestPointOnPlane;
		}

		#endregion

		#region #RNGUtils

		/// <summary> Random point inside of circle (uniform distribution) </summary>
		public static Vector2 RandomPointInCircle(Random rng)
		{
			Vector2 pointOnCircle = RandomPointOnCircle(rng);
			float r = Mathf.Sqrt((float)rng.NextDouble());
			return pointOnCircle * r;
		}

		/// <summary> Random point on circumference of circle </summary>
		public static Vector2 RandomPointOnCircle(Random rng)
		{
			float angle = (float)rng.NextDouble() * 2 * PI;
			float x = Mathf.Cos(angle);
			float y = Mathf.Sin(angle);
			return new Vector2(x, y);
		}

		/// <summary> Random point on surface of sphere (i.e. random direction) </summary>
		public static Vector3 RandomPointOnSphere(Random rng)
		{
			float x = RandomNormal(rng);
			float y = RandomNormal(rng);
			float z = RandomNormal(rng);
			return new Vector3(x, y, z).normalized;
		}

		/// <summary> Random point inside a triangle (with uniform distribution). </summary>
		public static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c, Random rng)
		{
			double randA = rng.NextDouble();
			double randB = rng.NextDouble();
			if (randA + randB > 1)
			{
				randA = 1 - randA;
				randB = 1 - randB;
			}

			return a + (b - a) * (float)randA + (c - a) * (float)randB;
		}

		/// <summary>
		///     Pick random index, weighted by the weights array.
		///     For example, if the array contains {1, 6, 3}...
		///     The possible indices would be (0, 1, 2)
		///     and the probabilities for these would be (1/10, 6/10, 3/10)
		/// </summary>
		public static int WeightedRandomIndex(Random rng, float[] weights)
		{
			float weightSum = 0;
			for (int i = 0; i < weights.Length; i++)
			{
				weightSum += weights[i];
			}

			float randomValue = (float)rng.NextDouble() * weightSum;
			float cumul = 0;

			for (int i = 0; i < weights.Length; i++)
			{
				cumul += weights[i];
				if (randomValue < cumul)
				{
					return i;
				}
			}

			return weights.Length - 1;
		}

		/// <summary> Randomly shuffles the elements of the given array </summary>
		public static void ShuffleArray<T>(T[] array, Random rng)
		{
			// wikipedia.org/wiki/Fisher–Yates_shuffle#The_modern_algorithm
			for (int i = 0; i < array.Length - 1; i++)
			{
				int randomIndex = rng.Next(i, array.Length);
				(array[randomIndex], array[i]) = (array[i], array[randomIndex]); // Swap
			}
		}

		/// <summary> Randomly shuffles the elements of the given list </summary>
		public static void ShuffleList<T>(IList<T> list, Random rng)
		{
			// wikipedia.org/wiki/Fisher–Yates_shuffle#The_modern_algorithm
			for (int i = 0; i < list.Count - 1; i++)
			{
				int randomIndex = rng.Next(i, list.Count);
				(list[randomIndex], list[i]) = (list[i], list[randomIndex]); // Swap
			}
		}

		/// <summary> Populates array with indices from 0 up to array.Length-1, but with the order shuffled randomly </summary>
		public static void PopulateWithUniqueRandomIndices(int[] array, Random rng)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}

			ShuffleArray(array, rng);
		}

		/// <summary> Create an array with indices from 0 up to Count-1, but with the order shuffled randomly </summary>
		public static int[] CreateUniqueRandomIndices(int count, Random rng)
		{
			int[] array = new int[count];
			PopulateWithUniqueRandomIndices(array, rng);
			return array;
		}

		#endregion

		#region #StatsAndCounting

		/// <summary>
		///     Returns a random value with normal distribution.
		///     The mean determines the 'centre' of the distribution.
		///     The standardDeviation controls the spread of the distribution (i.e. how likely it is to get values that are far from the mean).
		///     See https://www.desmos.com/calculator/0dnzmd0x0h for example.
		/// </summary>
		public static float RandomNormal(Random rng, float mean = 0, float standardDeviation = 1)
		{
			// Thanks to https://stackoverflow.com/a/6178290
			float theta = 2 * Mathf.PI * (float)rng.NextDouble();
			float rho = Mathf.Sqrt(-2 * Mathf.Log((float)rng.NextDouble()));
			float scale = standardDeviation * rho;
			return mean + scale * Mathf.Cos(theta);
		}

		/// <summary>
		///     Get min max float value from function.
		///     Example usage: Maths.GetMinMax((i) => particles[i].velocity.magnitude, particles.Count);
		/// </summary>
		public static (float min, float max, float mean) GetMinMaxMean(Func<int, float> GetValue, int count)
		{
			float min = float.MaxValue;
			float max = float.MinValue;
			float sum = 0;

			for (int i = 0; i < count; i++)
			{
				float val = GetValue(i);
				min = Math.Min(min, val);
				max = Math.Max(max, val);
				sum += val;
			}

			float mean = sum / count;
			return (min, max, mean);
		}

		public static (float min, float max, float mean) GetMinMaxMean(ReadOnlySpan<float> values)
		{
			float min = float.MaxValue;
			float max = float.MinValue;
			float sum = 0;

			foreach (float val in values)
			{
				min = Math.Min(min, val);
				max = Math.Max(max, val);
				sum += val;
			}

			float mean = sum / values.Length;
			return (min, max, mean);
		}

		#endregion

		#region #VectorUtils

		// Thanks to https://math.stackexchange.com/a/4112622
		// Calculates arbitrary normalized vector that is perpendicular to the given direction
		public static Vector3 CalculateOrthonormal(Vector3 dir)
		{
			float a = Mathf.Sign((Mathf.Sign(dir.x) + 0.5f) * (Mathf.Sign(dir.z) + 0.5f));
			float b = Mathf.Sign((Mathf.Sign(dir.y) + 0.5f) * (Mathf.Sign(dir.z) + 0.5f));
			return new Vector3(a * dir.z, b * dir.z, -a * dir.x - b * dir.y).normalized;
		}

		public static Vector3 Refract(Vector3 inDir, Vector3 normal, float iorA, float iorB)
		{
			// Thanks to https://graphics.stanford.edu/courses/cs148-10-summer/docs/2006--degreve--reflection_refraction.pdf
			float refractRatio = iorA / iorB;
			float cosAngleIn = -Vector3.Dot(inDir, normal);
			float sinSqrAngleOfRefraction = refractRatio * refractRatio * (1 - cosAngleIn * cosAngleIn);
			if (sinSqrAngleOfRefraction > 1) return Vector3.zero; // Ray is fully reflected, no refraction occurs

			Vector3 refractDir = refractRatio * inDir + (refractRatio * cosAngleIn - Mathf.Sqrt(1 - sinSqrAngleOfRefraction)) * normal;
			return refractDir;
		}

		public static Vector3 Reflect(Vector3 inDir, Vector3 normal) => inDir - 2 * Vector3.Dot(inDir, normal) * normal;

		public static float Fresnel(Vector3 inDir, Vector3 normal, float iorA, float iorB)
		{
			// Thanks to https://graphics.stanford.edu/courses/cs148-10-summer/docs/2006--degreve--reflection_refraction.pdf
			float refractRatio = iorA / iorB;
			float cosAngleIn = -Vector3.Dot(inDir, normal);
			float sinSqrAngleOfRefraction = refractRatio * refractRatio * (1 - cosAngleIn * cosAngleIn);
			if (sinSqrAngleOfRefraction > 1) return 1; // Ray is fully reflected, no refraction occurs

			float cosAngleOfRefraction = Mathf.Sqrt(1 - sinSqrAngleOfRefraction);
			float rPerp = (iorA * cosAngleIn - iorB * cosAngleOfRefraction) / (iorA * cosAngleIn + iorB * cosAngleOfRefraction);
			float rPar = (iorB * cosAngleIn - iorA * cosAngleOfRefraction) / (iorB * cosAngleIn + iorA * cosAngleOfRefraction);

			return (rPerp * rPerp + rPar * rPar) / 2;
		}

		public static Vector2 Rotate2D(Vector2 p, float angle)
		{
			float sinAngle = Sin(angle);
			float cosAngle = Cos(angle);
			Vector2 iHat = new(cosAngle, sinAngle);
			Vector2 jHat = new(-sinAngle, cosAngle);
			return iHat * p.x + jHat * p.y;
		}

		public static Vector2 RotateAroundPoint2D(Vector2 p, Vector2 anchor, float angle) => Rotate2D(p - anchor, angle) + anchor;

		public static Vector2 Abs(Vector2 vec) => new(MathF.Abs(vec.x), MathF.Abs(vec.y));

		#endregion

		#region SphericalGeometry

		/// <summary>
		///     Returns the length of the shortest arc between two points on the surface of a unit sphere.
		/// </summary>
		public static float ArcLengthBetweenPointsOnUnitSphere(Vector3 a, Vector3 b) =>
			// Thanks to https://www.movable-type.co.uk/scripts/latlong-vectors.html
			Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b));

		// Note: The following (simpler) approach works too, but is less precise for small angles:
		// return Mathf.Acos(Vector3.Dot(a, b));
		/// <summary>
		///     Returns the length of the shortest arc between two points on the surface of a sphere with the specified radius.
		/// </summary>
		public static float ArcLengthBetweenPointsOnSphere(Vector3 a, Vector3 b, float sphereRadius) => ArcLengthBetweenPointsOnUnitSphere(a.normalized, b.normalized) * sphereRadius;

		#endregion

		#region #Miscellaneous

		public static float Lerp(float a, float b, float t)
		{
			return a * (1 - t) + b * t;
		}

		public static float AbsoluteMax(float a, float b) => Mathf.Abs(a) > Mathf.Abs(b) ? a : b;

		public static (Vector2 centre, Vector2 size) BoundingBox(Vector2[] points)
		{
			if (points.Length == 0)
			{
				return (Vector2.zero, Vector2.zero);
			}

			Vector2 min = points[0];
			Vector2 max = points[0];
			for (int i = 1; i < points.Length; i++)
			{
				Vector2 p = points[i];
				min = new Vector2(Min(min.x, p.x), Min(min.y, p.y));
				max = new Vector2(Max(max.x, p.x), Max(max.y, p.y));
			}

			Vector2 centre = (min + max) / 2;
			Vector2 size = max - min;
			return (centre, size);
		}

		// Calculate point on sphere given longitude and latitude (in radians), and the radius of the sphere
		public static Vector3 CoordinateToSpherePoint(float latitude, float longitude, float radius = 1)
		{
			float y = Mathf.Sin(latitude);
			float r = Mathf.Cos(latitude); // radius of 2d circle cut through sphere at 'y'
			float x = Mathf.Sin(longitude) * r;
			float z = -Mathf.Cos(longitude) * r;

			return new Vector3(x, y, z) * radius;
		}

		public static (float longitude, float latitude) PointToCoordinate(Vector3 pointOnUnitSphere)
		{
			float latitude = Mathf.Asin(pointOnUnitSphere.y);
			float a = pointOnUnitSphere.x;
			float b = -pointOnUnitSphere.z;

			float longitude = Mathf.Atan2(a, b);
			return (longitude, latitude);
		}


		/// <summary>
		///     Rotates the point around the axis by the given angle (in radians)
		/// </summary>
		public static Vector3 RotateAroundAxis(Vector3 point, Vector3 axis, float angle) => RotateAroundAxis(point, axis, Mathf.Sin(angle), Mathf.Cos(angle));

		/// <summary>
		///     Rotates given vector by the rotation that aligns startDir with endDir
		/// </summary>
		public static Vector3 RotateBetweenDirections(Vector3 vector, Vector3 startDir, Vector3 endDir)
		{
			Vector3 rotationAxis = Vector3.Cross(startDir, endDir);
			float sinAngle = rotationAxis.magnitude;
			float cosAngle = Vector3.Dot(startDir, endDir);

			return RotateAroundAxis(vector, rotationAxis.normalized, cosAngle, sinAngle);
			// Note: this achieves the same as doing: 
			// return Quaternion.FromToRotation(originalDir, newDir) * point;
		}

		static Vector3 RotateAroundAxis(Vector3 point, Vector3 axis, float sinAngle, float cosAngle) =>
			// Rodrigues' rotation formula: https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
			point * cosAngle + Vector3.Cross(axis, point) * sinAngle + axis * Vector3.Dot(axis, point) * (1 - cosAngle);

		public static float SampleData(float[] data, double t)
		{
			if (data.Length == 0) return float.NaN;
			if (data.Length == 1) return data[0];

			// Sample
			int indexA = (int)(t * (data.Length - 1));
			int indexB = Math.Min(data.Length - 1, indexA + 1);
			float valA = data[indexA];
			float valB = data[indexB];

			// Interpolate
			double interval = 1.0 / (data.Length - 1);
			double intervalA = indexA * interval;
			double intervalB = indexB * interval;
			if (intervalA >= intervalB) return valB;
			double intervalT = (t - intervalA) / (intervalB - intervalA);
			return (float)(valA + (valB - valA) * intervalT);
		}

		// Calculates the real roots of a cubic equation (i.e. values of x satisfying: ax^3 + bx^2 + cx + d = 0)
		public static float[] RealCubicRoots(float a, float b, float c, float d)
		{
			float p = (3 * a * c - b * b) / (3 * a * a);
			float q = (2 * b * b * b - 9 * a * b * c + 27 * a * a * d) / (27 * a * a * a);
			float tToX = -b / (3 * a);
			float rootTest = 4 * p * p * p + 27 * q * q;
			bool onlyOneRealRoot = (rootTest > 0 && p < 0) || p > 0;

			if (onlyOneRealRoot && p > 0)
			{
				float asinh = Asinh(3 * q / (2 * p) * Sqrt(3 / p));
				float root0 = -2 * Sqrt(p / 3) * Sinh(1 / 3f * asinh) + tToX;
				return new[] { root0, float.NaN, float.NaN };
			}

			if (onlyOneRealRoot)
			{
				float acosh = Acosh(-3 * Math.Abs(q) / (2 * p) * Sqrt(-3 / p));
				float root0 = -2 * Math.Sign(q) * Sqrt(-p / 3) * Cosh(1 / 3f * acosh) + tToX;
				return new[] { root0, float.NaN, float.NaN };
			}

			return new[] { CalcRoot(0), CalcRoot(1), CalcRoot(2) };

			float CalcRoot(int k) // where k is 0, 1, or 2
			{
				float acos = Acos(3 * q / (2 * p) * Sqrt(-3 / p));
				float cos = Cos(1 / 3f * acos - 2 * PI * k / 3);
				return 2 * Sqrt(-p / 3) * cos + tToX;
			}
		}

		public static bool ApproximatelyEqual(float a, float b) => Math.Abs(a - b) < Epsilon;
		public static float Min(float a, float b) => a < b ? a : b;
		public static float Max(float a, float b) => a > b ? a : b;


		/// <summary>
		///     Returns n points distributed reasonably evenly on a sphere.
		///     Uses fibonacci spiral technique.
		/// </summary>
		public static Vector3[] GetPointsOnSphereSurface(int numPoints, float radius = 1)
		{
			// Thanks to https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere/44164075#44164075
			Vector3[] points = new Vector3[numPoints];
			const double goldenRatio = 1.618033988749894; // (1 + sqrt(5)) / 2
			const double angleIncrement = Math.PI * 2 * goldenRatio;

			Parallel.For(0, numPoints, i =>
			{
				double t = (double)i / numPoints;
				double inclination = Acos(1 - 2 * t);
				double azimuth = angleIncrement * i;

				double x = Sin(inclination) * Cos(azimuth);
				double y = Sin(inclination) * Sin(azimuth);
				double z = Cos(inclination);
				points[i] = new Vector3((float)x, (float)y, (float)z) * radius;
			});
			return points;
		}

		public static float Clamp01(float t) => Clamp(t, 0, 1);
		public static float Square(float x) => x * x;
		public static float Cube(float x) => x * x * x;
		public static float Quart(float x) => x * x * x * x;
		public static float Abs(float x) => Math.Abs(x);

		public static int IntLog2(int value)
		{
			int result = 0;
			while (value > 1)
			{
				value >>= 1;
				result++;
			}

			return result;
		}

		public static int TwosComplement(uint unsignedValue, int numBits)
		{
			if (numBits < 32)
			{
				uint unsignedRange = 1u << numBits;
				uint firstNegativeValue = unsignedRange >> 1;

				if (unsignedValue >= firstNegativeValue)
				{
					return (int)(unsignedValue - unsignedRange);
				}
			}

			return (int)unsignedValue;
		}

		#endregion
	}
}