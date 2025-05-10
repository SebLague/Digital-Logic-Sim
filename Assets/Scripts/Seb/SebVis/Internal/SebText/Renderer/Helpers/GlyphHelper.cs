using System;
using System.Collections.Generic;
using Seb.Vis.Text.FontLoading;
using UnityEngine;

namespace Seb.Vis.Text.Rendering.Helpers
{
	public static class GlyphHelper
	{
		const float antiAliasPadding = 0.005f; // todo: move to shader

		public static TextRenderData CreateRenderData(Glyph[] uniqueCharacters)
		{
			TextRenderData renderData = new();

			for (int charIndex = 0; charIndex < uniqueCharacters.Length; charIndex++)
			{
				Glyph glyph = uniqueCharacters[charIndex];
				Vector2[][] contours = glyph.Contours;

				TextRenderData.GlyphData glyphData = new()
				{
					SizeEm = glyph.Size + Vector2.one * antiAliasPadding,
					ContourDataOffset = renderData.GlyphMetaData.Count,
					PointDataOffset = renderData.BezierPoints.Count,
					NumContours = contours.Length
				};

				renderData.AllGlyphData.Add(glyphData);
				renderData.GlyphMetaData.Add(renderData.BezierPoints.Count);
				renderData.GlyphMetaData.Add(contours.Length);

				foreach (Vector2[] contour in contours)
				{
					renderData.GlyphMetaData.Add(contour.Length - 1);
					for (int i = 0; i < contour.Length; i++)
					{
						renderData.BezierPoints.Add(contour[i] - glyph.Centre);
					}
				}
			}

			return renderData;
		}

		// Process raw glyph data to construct the contours
		// Output is an array of contours (each contour being an array of points forming a quadratic bezier path)
		// The contours are in 'em' units.
		public static Vector2[][] ProcessContours(int[] contourEndIndices, FontParser.Point[] points, float scale)
		{
			const bool convertStraightLinesToBezier = true;

			int startPointIndex = 0;
			int contourCount = contourEndIndices.Length;

			List<Vector2[]> contours = new();

			for (int contourIndex = 0; contourIndex < contourCount; contourIndex++)
			{
				int contourEndIndex = contourEndIndices[contourIndex];
				int numPointsInContour = contourEndIndex - startPointIndex + 1;
				Span<FontParser.Point> contourPoints = points.AsSpan(startPointIndex, numPointsInContour);

				List<Vector2> reconstructedPoints = new();
				List<Vector2> onCurvePoints = new();

				// Get index of first on-curve point (seems to not always be first point for whatever reason)
				int firstOnCurvePointIndex = 0;
				for (int i = 0; i < contourPoints.Length; i++)
				{
					if (contourPoints[i].OnCurve)
					{
						firstOnCurvePointIndex = i;
						break;
					}
				}

				for (int i = 0; i < contourPoints.Length; i++)
				{
					FontParser.Point curr = contourPoints[(i + firstOnCurvePointIndex + 0) % contourPoints.Length];
					FontParser.Point next = contourPoints[(i + firstOnCurvePointIndex + 1) % contourPoints.Length];

					reconstructedPoints.Add(new Vector2(curr.X * scale, curr.Y * scale));
					if (curr.OnCurve) onCurvePoints.Add(new Vector2(curr.X * scale, curr.Y * scale));
					bool isConsecutiveOffCurvePoints = !curr.OnCurve && !next.OnCurve;
					bool isStraightLine = curr.OnCurve && next.OnCurve;

					if (isConsecutiveOffCurvePoints || (isStraightLine && convertStraightLinesToBezier))
					{
						bool onCurve = isConsecutiveOffCurvePoints;
						float newX = (curr.X + next.X) / 2.0f * scale;
						float newY = (curr.Y + next.Y) / 2.0f * scale;
						reconstructedPoints.Add(new Vector2(newX, newY));
						if (onCurve) onCurvePoints.Add(new Vector2(newX, newY));
					}
				}

				reconstructedPoints.Add(reconstructedPoints[0]);
				reconstructedPoints = MakeMonotonic(reconstructedPoints);


				contours.Add(reconstructedPoints.ToArray());

				startPointIndex = contourEndIndex + 1;
			}

			return contours.ToArray();
		}

		static List<Vector2> MakeMonotonic(List<Vector2> original)
		{
			List<Vector2> monotonic = new(original.Count) { original[0] };

			for (int i = 0; i < original.Count - 1; i += 2)
			{
				Vector2 p0 = original[i];
				Vector2 p1 = original[i + 1];
				Vector2 p2 = original[i + 2];

				if (p1.y < Mathf.Min(p0.y, p2.y) || p1.y > Mathf.Max(p0.y, p2.y))
				{
					(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) split = SplitAtTurningPointY(p0, p1, p2);
					monotonic.Add(split.a1);
					monotonic.Add(split.a2);
					monotonic.Add(split.b1);
					monotonic.Add(split.b2);
				}
				else
				{
					monotonic.Add(p1);
					monotonic.Add(p2);
				}
			}

			return monotonic;
		}

		static (Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) SplitAtTurningPointY(Vector2 p0, Vector2 p1, Vector2 p2)
		{
			Vector2 a = p0 - 2 * p1 + p2;
			Vector2 b = 2 * (p1 - p0);
			Vector2 c = p0;

			// Calculate turning point by setting gradient.y to 0: 2at + b = 0; therefore t = -b / 2a
			float turningPointT = -b.y / (2 * a.y);
			Vector2 turningPoint = a * turningPointT * turningPointT + b * turningPointT + c;

			// Calculate the new p1 point for curveA with points: p0, p1A, turningPoint
			// This is done by saying that p0 + gradient(t=0) * ? = p1A = (p1A.x, turningPoint.y)
			// Solve for lambda using the known turningPoint.y, and then solve for p1A.x
			float lambdaA = (turningPoint.y - p0.y) / b.y;
			float p1A_x = p0.x + b.x * lambdaA;

			// Calculate the new p1 point for curveB with points: turningPoint, p1B, p2
			// This is done by saying that p2 + gradient(t=1) * ? = p1B = (p1B.x, turningPoint.y)
			// Solve for lambda using the known turningPoint.y, and then solve for p1B.x
			float lambdaB = (turningPoint.y - p2.y) / (2 * a.y + b.y);
			float p1B_x = p2.x + (2 * a.x + b.x) * lambdaB;

			return (new Vector2(p1A_x, turningPoint.y), turningPoint, new Vector2(p1B_x, turningPoint.y), p2);
		}


		[Serializable]
		public class TextRenderData
		{
			public List<Vector2> BezierPoints = new();

			public List<GlyphData> AllGlyphData = new();

			// Metadata for each glyph: bezier data offset, num contours, contour length/s
			public List<int> GlyphMetaData = new();

			[Serializable]
			public struct GlyphData
			{
				public int NumContours;
				public int ContourDataOffset;
				public int PointDataOffset;
				public Vector2 SizeEm;
			}
		}
	}
}