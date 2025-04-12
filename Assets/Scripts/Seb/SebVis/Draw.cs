using System;
using Seb.Vis.Internal;
using Seb.Vis.Text.FontLoading;
using Seb.Vis.Text.Rendering;
using UnityEngine;

namespace Seb.Vis
{
	// (Partial) Draw class: contains the public drawing functions
	public static partial class Draw
	{
		// Start a new layer. Within each layer, shapes are drawn first, followed by text.
		// So, if shapes need to appear on top of some text, a new layer must be started.
		public static void StartLayer(Vector2 offset, float scale, bool useScreenSpace)
		{
			if (!isInitSinceCleanup) Init();

			LayerInfo layerInfo = new(offset, scale, useScreenSpace);
			shapeDrawer.StartNewLayer(layerInfo);
			textDrawer.StartNewLayer(layerInfo);
			layers.Push(layerInfo);
		}

		// Starts a new layer if not currently on a layer with matching parameters.
		public static void StartLayerIfNotInMatching(Vector2 offset, float scale, bool useScreenSpace)
		{
			bool createNewLayer = layers.Count == 0;

			if (layers.Count > 0)
			{
				LayerInfo currLayer = layers.Peek();
				createNewLayer = currLayer.offset != offset || currLayer.scale != scale || currLayer.useScreenSpace != useScreenSpace;
			}

			if (createNewLayer) StartLayer(offset, scale, useScreenSpace);
		}

		public static MaskScope BeginMaskScope(Vector2 maskBoundsMin, Vector2 maskBoundsMax)
		{
			MaskScope maskScope = maskScopes.CreateScope();
			maskScope.Init(maskBoundsMin, maskBoundsMax);
			return maskScope;
		}

		public static (Vector2 min, Vector2 max) GetActiveMaskMinMax() => (activeMaskMin, activeMaskMax);
		// ------ Core Draw Functions ------

		public static void Text(FontType font, string text, float fontSize, Vector2 pos, Anchor anchor, Color col, float lineSpacing = 1)
		{
			if (fontSize <= 0 || col.a == 0 || string.IsNullOrEmpty(text)) return;

			FontData fontData = defaultFontsData[(int)font];
			TextDrawData data = new(fontData, text, fontSize, lineSpacing, pos, anchor, col, activeMaskMin, activeMaskMax);
			textDrawer.AddToLayer(data);
		}

		public static void Text(FontType font, char[] text, int textLength, float fontSize, Vector2 pos, Anchor anchor, Color col, float lineSpacing = 1)
		{
			if (fontSize <= 0 || col.a == 0 || text == null || text.Length == 0 || textLength == 0) return;

			FontData fontData = defaultFontsData[(int)font];
			TextDrawData data = new(fontData, text, textLength, fontSize, lineSpacing, pos, anchor, col, activeMaskMin, activeMaskMax);
			textDrawer.AddToLayer(data);
		}

		public static void Triangle(Vector2 a, Vector2 b, Vector2 c, Color col)
		{
			if (col.a == 0) return;

			ShapeData data = ShapeData.CreateTriangle(a, b, c, col);
			shapeDrawer.AddToLayer(data);
		}

		public static void Triangle(Vector2 centre, Vector2 dir, float height, float scale, Color col)
		{
			if (col.a == 0 || scale == 0 || height == 0) return;

			Vector2 perp = new(-dir.y, dir.x);
			Vector2 tip = centre + dir * scale;
			Vector2 top = centre - (dir + perp * height) * scale;
			Vector2 bottom = centre - (dir - perp * height) * scale;

			ShapeData data = ShapeData.CreateTriangle(tip, top, bottom, col);
			shapeDrawer.AddToLayer(data);
		}

		public static void Diamond(Vector2 pos, Vector2 size, Color col)
		{
			if (size.x == 0 || size.y == 0 || col.a == 0) return;

			ShapeData data = ShapeData.CreateDiamond(pos, size, col, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void SatValQuad(Vector2 centre, Vector2 size, float hue)
		{
			if (size.x == 0 || size.y == 0) return;

			ShapeData data = ShapeData.CreateSatVal(centre, size, hue, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void HueQuad(Vector2 centre, Vector2 size)
		{
			if (size.x == 0 || size.y == 0) return;

			ShapeData data = ShapeData.CreateHueQuad(centre, size, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void Point(Vector2 centre, float radius, Color col)
		{
			if (radius == 0 || col.a == 0) return;

			ShapeData data = ShapeData.CreatePoint(centre, Vector2.one * radius, col, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void PointOutline(Vector2 centre, float radius, float thickness, Color col)
		{
			if (radius == 0 || thickness == 0 || col.a == 0) return;

			ShapeData data = ShapeData.CreatePointOutline(centre, radius, thickness, col, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void Ellipse(Vector2 centre, Vector2 size, Color col)
		{
			if (size.x == 0 || size.y == 0 || col.a == 0) return;

			ShapeData data = ShapeData.CreatePoint(centre, size, col, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void ShapeBatch(ShapeData[] shapes)
		{
			shapeDrawer.AddToLayer(shapes);
		}

		public static void Quad(Vector2 centre, Vector2 size, Color col)
		{
			if (size.x == 0 || size.y == 0 || col.a == 0) return;

			ShapeData data = ShapeData.CreateQuad(centre, size, col, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		public static void Line(Vector2 a, Vector2 b, float thickness, Color col, float t = 1)
		{
			if (thickness == 0 || t == 0 || a == b || col.a == 0) return;

			if (t < 1) b = Vector2.Lerp(a, b, t);
			ShapeData data = ShapeData.CreateLine(a, b, thickness, col, activeMaskMin, activeMaskMax);
			shapeDrawer.AddToLayer(data);
		}

		// ------ Composite Draw Functions ------

		public static void LinePath(Vector2[] points, float thickness, Color col, float animT = 1)
		{
			if (col.a == 0 || thickness == 0 || animT <= 0) return;

			float totalLength = 0;
			for (int i = 0; i < points.Length - 1; i++)
			{
				totalLength += Vector2.Distance(points[i], points[i + 1]);
			}

			float drawLength = totalLength * animT;
			float lengthDrawn = 0;

			for (int i = 0; i < points.Length - 1; i++)
			{
				bool exit = false;
				float segLength = Vector2.Distance(points[i], points[i + 1]);
				if (lengthDrawn + segLength > drawLength)
				{
					segLength = drawLength - lengthDrawn;
					exit = true;
				}

				Vector2 a = points[i];
				Vector2 b = points[i + 1];
				b = a + (b - a).normalized * segLength;
				Line(a, b, thickness, col);
				lengthDrawn += segLength;
				if (exit)
				{
					break;
				}
			}
		}

		public static void Arrow(Vector2 start, Vector2 end, float thickness, Color col, float t = 1)
		{
			Arrow(start, end, thickness, thickness * 3.5f, 32, col, t);
		}

		public static void Arrow(Vector2 start, Vector2 end, float thickness, float headLength, float headAngleDeg, Color col, float t = 1)
		{
			Vector2 animEnd = Vector2.Lerp(start, end, t);

			// Calculate arrow head end points
			Vector2 dir = (end - start).normalized;
			Vector2 perp = new(-dir.y, dir.x);
			float headT = Mathf.InverseLerp(0.3f, 1, t);
			float angle = Mathf.Deg2Rad * headAngleDeg * headT;

			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);
			Vector2 headDirA = -dir * cos + perp * sin;
			Vector2 headDirB = -dir * cos - perp * sin;

			if (headAngleDeg < 90)
			{
				float maxHeadLength = (animEnd - start).magnitude / cos;
				headLength = Mathf.Min(headLength, maxHeadLength);
			}

			Vector2 headEndA = animEnd + headDirA * headLength;
			Vector2 headEndB = animEnd + headDirB * headLength;

			// Draw arrow shaft
			Line(start, animEnd, thickness, col);
			// Draw arrow head
			Line(animEnd, headEndA, thickness, col);
			Line(animEnd, headEndB, thickness, col);
			Line(headEndA, headEndB, thickness, col);
			// Fill arrow head
			if ((headEndA - headEndB).magnitude > thickness)
			{
				Triangle(animEnd, headEndA, headEndB, col);
			}
		}


		public static void QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float thickness, Color col, int resolution)
		{
			if (resolution <= 0) return;
			resolution += 1;

			Vector2 a = p0 - 2 * p1 + p2;
			Vector2 b = 2 * (p1 - p0);
			Vector2 c = p0;

			Span<Vector2> points = stackalloc Vector2[resolution];
			points[0] = p0;

			for (int i = 1; i < resolution; i++)
			{
				float t = i / (resolution - 1f);
				points[i] = a * t * t + b * t + c;
			}

			for (int i = 0; i < points.Length - 1; i++)
			{
				Line(points[i], points[i + 1], thickness, col);
			}
		}

		public static void DataLine(float[] yVals, float xStart, float xEnd, float yMul, float thickness, Color col, float animT = 1)
		{
			if (thickness == 0 || col.a == 0 || animT <= 0) return;

			for (int i = 1; i < yVals.Length; i++)
			{
				float t = i / (yVals.Length - 1f);
				Vector2 a = GetPoint(i - 1);
				Vector2 b = GetPoint(i);

				if (t > animT)
				{
					float tPrev = (i - 1f) / (yVals.Length - 1f);
					float localT = Mathf.InverseLerp(tPrev, t, animT);
					b = Vector2.Lerp(a, b, localT);
				}

				Line(a, b, thickness, col);
				if (t > animT) break;
			}

			Vector2 GetPoint(int i)
			{
				float y = yVals[i] * yMul;
				float t = i / (yVals.Length - 1f);
				float x = Mathf.Lerp(xStart, xEnd, t);
				return new Vector2(x, y);
			}
		}

		public static void DataLine(double[] yVals, float xStart, float xEnd, float yMul, float thickness, Color col, float animT = 1)
		{
			if (thickness == 0 || col.a == 0 || animT <= 0) return;

			for (int i = 1; i < yVals.Length; i++)
			{
				float t = i / (yVals.Length - 1f);
				Vector2 a = GetPoint(i - 1);
				Vector2 b = GetPoint(i);

				if (t > animT)
				{
					float tPrev = (i - 1f) / (yVals.Length - 1f);
					float localT = Mathf.InverseLerp(tPrev, t, animT);
					b = Vector2.Lerp(a, b, localT);
				}

				Line(a, b, thickness, col);
				if (t > animT) break;
			}

			Vector2 GetPoint(int i)
			{
				float y = (float)(yVals[i] * yMul);
				float t = i / (yVals.Length - 1f);
				float x = Mathf.Lerp(xStart, xEnd, t);
				return new Vector2(x, y);
			}
		}

		public static void Cross(Vector3 centre, float length, float thickness, Color col, float animT = 1)
		{
			float d = 0.7071f; // 1 / sqrt(2)
			Vector3 startA = centre - new Vector3(d, d) * length / 2 * animT;
			Vector3 endA = centre + new Vector3(d, d) * length / 2 * animT;
			Line(startA, endA, thickness * animT, col);

			Vector3 startB = centre - new Vector3(d, -d) * length / 2 * animT;
			Vector3 endB = centre + new Vector3(d, -d) * length / 2 * animT;
			Line(startB, endB, thickness * animT, col);
		}

		public static void GridLines(Vector2 centre, Vector2 size, int numCellsX, int numCellsY, float thickness, Color col)
		{
			int numLinesX = numCellsX + 1;
			int numLinesY = numCellsY + 1;
			Vector2 bottomLeft = centre - size / 2;

			for (int y = 0; y < numLinesY; y++)
			{
				float t = y / (numLinesY - 1f);
				Vector2 a = bottomLeft + Vector2.up * size.y * t;
				Vector2 b = a + Vector2.right * size.x;
				Line(a, b, thickness, col);
			}

			for (int x = 0; x < numLinesX; x++)
			{
				float t = x / (numLinesX - 1f);
				Vector2 a = bottomLeft + Vector2.right * size.x * t;
				Vector2 b = a + Vector2.up * size.y;
				Line(a, b, thickness, col);
			}
		}

		public static void GridSquares(Vector2 centre, Vector2 boundsSize, Vector2Int squareCount, float squareScaleT = 1)
		{
			Vector2 squareSize = new(boundsSize.x / squareCount.x, boundsSize.y / squareCount.y);
			Vector2 bottomLeft = centre - boundsSize / 2;
			Color col = Color.white;

			for (int y = 0; y < squareCount.y; y++)
			{
				for (int x = 0; x < squareCount.x; x++)
				{
					float posX = bottomLeft.x + squareSize.x * (0.5f + x);
					float posY = bottomLeft.y + squareSize.y * (0.5f + y);
					Vector2 pos = new Vector3(posX, posY);

					Quad(pos, squareSize * squareScaleT, col);
				}
			}
		}

		public static void QuadMinMax(Vector2 min, Vector2 max, Color col)
		{
			Quad((min + max) / 2, max - min, col);
		}

		public static void QuadOutlineMinMax(Vector2 min, Vector2 max, float thickness, Color col)
		{
			QuadOutline((min + max) / 2, max - min, thickness, col);
		}

		public static void QuadOutline(Vector2 centre, Vector2 size, float thickness, Color col)
		{
			Vector2 bottomLeft = centre - size / 2;
			Vector2 topRight = centre + size / 2;
			Vector2 topLeft = new(bottomLeft.x, topRight.y);
			Vector2 bottomRight = new(topRight.x, bottomLeft.y);

			Line(bottomLeft, topLeft, thickness, col);
			Line(topLeft, topRight, thickness, col);
			Line(topRight, bottomRight, thickness, col);
			Line(bottomRight, bottomLeft, thickness, col);
		}

		// ------ Bounds ------
		public static Vector2 CalculateTextBoundsSize(ReadOnlySpan<char> text, float fontSize, FontType font, float lineSpacing = 1) => CalculateTextBounds(text, font, fontSize, Vector2.zero, Anchor.Centre, lineSpacing).Size;

		public static TextRenderer.BoundingBox CalculateTextBounds(ReadOnlySpan<char> text, FontType font, float fontSize, Vector2 pos, Anchor anchor, float lineSpacing = 1)
		{
			FontData fontData = defaultFontsData[(int)font];
			return TextRenderer.CalculateWorldBounds(text, fontData, new TextRenderer.LayoutSettings(fontSize, lineSpacing, 1, 1), pos, anchor);
		}

		public static bool IsPointInsideActiveMask(Vector2 p)
		{
			if (maskScopes.TryGetCurrentScope(out MaskScope mask))
			{
				return p.x >= mask.boundsMin.x && p.x <= mask.boundsMax.x && p.y >= mask.boundsMin.y && p.y <= mask.boundsMax.y;
			}

			return true;
		}

		// ------ Reserve and Modify Functions ------

		public static ID ReserveLine()
		{
			shapeDrawer.AddToLayer(ShapeData.CreateLine(Vector2.zero, Vector2.zero, 0, Color.clear, activeMaskMin, activeMaskMax));
			return new ID(shapeDrawer.CurrDrawDataIndex);
		}

		public static void ModifyLine(ID id, Vector2 start, Vector2 end, float thickness, Color col)
		{
			shapeDrawer.allDrawData[id.index] = ShapeData.CreateLine(start, end, thickness, col, activeMaskMin, activeMaskMax);
		}

		public static ID ReserveQuad()
		{
			shapeDrawer.AddToLayer(ShapeData.CreateQuad(Vector2.zero, Vector2.zero, Color.clear, activeMaskMin, activeMaskMax));
			return new ID(shapeDrawer.CurrDrawDataIndex);
		}

		public static void ModifyQuad(ID id, Vector2 centre, Vector2 size, Color col)
		{
			shapeDrawer.allDrawData[id.index] = ShapeData.CreateQuad(centre, size, col, activeMaskMin, activeMaskMax);
		}

		public readonly struct ID
		{
			public readonly int index;

			public ID(int index)
			{
				this.index = index;
			}
		}

		// ------ Internal Draw Functions ------
	}
}