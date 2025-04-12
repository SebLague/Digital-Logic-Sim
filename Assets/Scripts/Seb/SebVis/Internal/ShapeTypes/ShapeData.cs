using System;
using UnityEngine;

namespace Seb.Vis.Internal
{
	public enum ShapeType
	{
		Line = 0,
		Point = 1,
		Quad = 2,
		Triangle = 3,
		SatVal = 4,
		HueQuad = 5,
		Diamond = 6,
		PointOutline = 7
	}

	public struct ShapeData
	{
		public readonly int type;
		public readonly Vector2 a;
		public readonly Vector2 b;
		public readonly float c;

		public Color col;
		public readonly Vector2 maskMin;
		public readonly Vector2 maskMax;

		public ShapeData(ShapeType type, Vector2 a, Vector2 b, float c, Color col)
		{
			this.type = (int)type;
			this.a = a;
			this.b = b;
			this.c = c;
			this.col = col;
			maskMin = Vector2.one * float.MinValue;
			maskMax = Vector2.one * float.MaxValue;
		}

		public ShapeData(ShapeType type, Vector2 a, Vector2 b, float c, Color col, Vector2 maskMin, Vector2 maskMax)
		{
			this.type = (int)type;
			this.a = a;
			this.b = b;
			this.c = c;
			this.col = col;
			this.maskMin = maskMin;
			this.maskMax = maskMax;
		}

		public static ShapeData CreateLine(Vector2 a, Vector2 b, float thickness, Color col, Vector2 maskMin, Vector2 maskMax) => new(ShapeType.Line, a, b, thickness, col, maskMin, maskMax);

		public static ShapeData CreatePoint(Vector2 centre, Vector2 size, Color col, Vector2 maskMin, Vector2 maskMax) => new(ShapeType.Point, centre, size, 0, col, maskMin, maskMax);

		public static ShapeData CreatePointOutline(Vector2 centre, float radius, float thickness, Color col, Vector2 maskMin, Vector2 maskMax)
		{
			float radiusOuter = radius + thickness / 2;
			float radiusInner = radius - thickness / 2;
			float radiusInnerT = radiusInner / radiusOuter;
			return new ShapeData(ShapeType.PointOutline, centre, Vector2.one * radiusOuter, radiusInnerT, col, maskMin, maskMax);
		}

		public static ShapeData CreateQuad(Vector2 centre, Vector2 size, Color col, Vector2 maskMin, Vector2 maskMax) => new(ShapeType.Quad, centre, size, 0, col, maskMin, maskMax);

		public static ShapeData CreateSatVal(Vector2 centre, Vector2 size, float value, Vector2 maskMin, Vector2 maskMax) => new(ShapeType.SatVal, centre, size, value, Color.clear, maskMin, maskMax);

		public static ShapeData CreateHueQuad(Vector2 centre, Vector2 size, Vector2 maskMin, Vector2 maskMax) => new(ShapeType.HueQuad, centre, size, 0, Color.clear, maskMin, maskMax);

		public static ShapeData CreateDiamond(Vector2 centre, Vector2 size, Color col, Vector2 maskMin, Vector2 maskMax) => new(ShapeType.Diamond, centre, size, 0, col, maskMin, maskMax);

		public static ShapeData CreateTriangle(Vector2 a, Vector2 b, Vector2 c, Color col) => new(ShapeType.Triangle, a, b, PackFloats(c.x, c.y), col);

		static float PackFloats(float a, float b)
		{
			uint a16 = Mathf.FloatToHalf(a);
			uint b16 = Mathf.FloatToHalf(b);
			uint v = (a16 << 16) | b16;

			return BitConverter.Int32BitsToSingle((int)v);
		}
	}
}