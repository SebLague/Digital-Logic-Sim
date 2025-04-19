using UnityEngine;

namespace Seb.Types
{
	public readonly struct Bounds2D
	{
		public readonly Vector2 Min;
		public readonly Vector2 Max;

		public readonly Vector2 Size => Max - Min;
		public readonly Vector2 Centre => (Min + Max) / 2;

		public readonly float Width => Size.x;
		public readonly float Height => Size.y;

		public readonly Vector2 BottomLeft => Min;
		public readonly Vector2 TopRight => Max;
		public readonly Vector2 BottomRight => new(Max.x, Min.y);
		public readonly Vector2 TopLeft => new(Min.x, Max.y);
		public readonly Vector2 CentreLeft => new(Min.x, (Min.y + Max.y) / 2);
		public readonly Vector2 CentreRight => new(Max.x, (Min.y + Max.y) / 2);
		public readonly Vector2 CentreTop => new((Min.x + Max.x) / 2, Max.y);
		public readonly Vector2 CentreBottom => new((Min.x + Max.x) / 2, Min.y);

		public readonly float Left => Min.x;
		public readonly float Right => Max.x;
		public readonly float Top => Max.y;
		public readonly float Bottom => Min.y;

		public Bounds2D(Vector2 min, Vector2 max)
		{
			Min = min;
			Max = max;
		}

		public static Bounds2D CreateFromCentreAndSize(Vector2 centre, Vector2 size) => new(centre - size / 2, centre + size / 2);

		public static Bounds2D CreateFromTopLeftAndSize(Vector2 topLeft, Vector2 size)
		{
			Vector2 min = new(topLeft.x, topLeft.y - size.y);
			return new Bounds2D(min, min + size);
		}

		public static Bounds2D CreateEmpty() => new(Vector2.one * float.MaxValue, Vector2.one * float.MinValue);

		public static Bounds2D Translate(Bounds2D a, Vector2 offset) => new(a.Min + offset, a.Max + offset);

		public static Bounds2D Grow(Bounds2D a, Bounds2D b) => new(Vector2.Min(a.Min, b.Min), Vector2.Max(a.Max, b.Max));

		public static Bounds2D Grow(Bounds2D bounds, Vector2 p) => new(Vector2.Min(bounds.Min, p), Vector2.Max(bounds.Max, p));

		public static Bounds2D Grow(Bounds2D bounds, float growAmount)
		{
			Vector2 delta = Vector2.one * (growAmount * 0.5f);
			return new Bounds2D(bounds.Min - delta, bounds.Max + delta);
		}

		public static Bounds2D Shrink(Bounds2D bounds, float amount)
		{
			Vector2 delta = Vector2.one * (amount * 0.5f);
			return new Bounds2D(bounds.Min + delta, bounds.Max - delta);
		}

		public static (Bounds2D left, Bounds2D right) SplitVertical(Bounds2D bounds, float splitT)
		{
			float widthLeft = bounds.Width * splitT;
			float widthRight = bounds.Width * (1 - splitT);
			Bounds2D left = CreateFromTopLeftAndSize(bounds.TopLeft, new Vector2(widthLeft, bounds.Height));
			Bounds2D right = CreateFromTopLeftAndSize(left.TopRight, new Vector2(widthRight, bounds.Height));
			return (left, right);
		}

		public bool EntirelyInside(Bounds2D parent) => Min.x >= parent.Min.x && Max.x <= parent.Max.x && Min.y >= parent.Min.y && Max.y <= parent.Max.y;

		public bool Overlaps(Bounds2D other) => !(other.Min.x > Max.x || other.Max.x < Min.x || other.Min.y > Max.y || other.Max.y < Min.y);

		public bool PointInBounds(Vector2 p) => p.x >= Min.x && p.x <= Max.x && p.y >= Min.y && p.y <= Max.y;

		public float DstToCorner(Vector2 p)
		{
			float centreOffsetX = Mathf.Abs(Centre.x - p.x);
			float centreOffsetY = Mathf.Abs(Centre.y - p.y);
			float edgeOffsetX = Mathf.Abs(Size.x / 2 - centreOffsetX);
			float edgeOffsetY = Mathf.Abs(Size.y / 2 - centreOffsetY);
			return Mathf.Sqrt(edgeOffsetX * edgeOffsetX + edgeOffsetY * edgeOffsetY);
		}

		public float Area => Size.x * Size.y;
	}
}