using UnityEngine;

namespace Seb.Vis.UI
{
	public static class UILayoutHelper
	{
		public const float DefaultSpacing = 0.5f;

		public static (Vector2 size, Vector2 centre) HorizontalLayout(int numElements, int elementIndex, Vector2 centre, Vector2 size, float spacing = DefaultSpacing)
		{
			float spaceTotal = (numElements - 1) * spacing;
			float elementWidth = (size.x - spaceTotal) / numElements;
			float posX = centre.x - size.x / 2 + elementWidth / 2 + (spacing + elementWidth) * elementIndex;
			return (new Vector2(elementWidth, size.y), new Vector2(posX, centre.y));
		}

		public static (Vector2 size, Vector2 centre) HorizontalLayout(int numElements, int elementIndex, Vector2 pos, Vector2 size, Anchor anchor, float spacing = DefaultSpacing)
		{
			Vector2 centre = CalculateCentre(pos, size, anchor);
			return HorizontalLayout(numElements, elementIndex, centre, size, spacing);
		}

		public static Vector2 CalculateCentre(Vector2 pos, Vector2 size, Anchor anchor)
		{
			return pos + anchor switch
			{
				Anchor.Centre => Vector2.zero,
				Anchor.CentreLeft => new Vector2(size.x, 0) / 2,
				Anchor.CentreRight => new Vector2(-size.x, 0) / 2,
				Anchor.TopLeft => new Vector2(size.x, -size.y) / 2,
				Anchor.TopRight => new Vector2(-size.x, -size.y) / 2,
				Anchor.CentreTop => new Vector2(0, -size.y) / 2,
				Anchor.BottomLeft => size / 2,
				Anchor.BottomRight => new Vector2(-size.x, size.y) / 2,
				Anchor.CentreBottom => new Vector2(0, size.y) / 2,
				_ => Vector2.zero
			};
		}
	}
}