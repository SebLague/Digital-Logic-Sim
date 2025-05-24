using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Game
{
	public static class GridHelper
	{
		public static Vector2 SnapMovingElementToGrid(IMoveable element, Vector2 offset, bool allowCentreSnapX = false, bool allowCentreSnapY = false)
		{
			Vector2 anchorOffset = element.SnapPoint - element.Position;
			return SnapMovingElementToGrid(element.MoveStartPosition + offset, anchorOffset, allowCentreSnapX, allowCentreSnapY);
		}

		public static Vector2 SnapMovingElementToGrid(Vector2 centrePos, Vector2 anchorPosLocal, bool allowCentreSnapX = false, bool allowCentreSnapY = false)
		{
			Vector2 anchorPos = centrePos + anchorPosLocal;
			Vector2 anchorPos_Snapped = SnapToGrid(anchorPos, allowCentreSnapX, allowCentreSnapY);
			Vector2 centrePos_Snapped = anchorPos_Snapped - anchorPosLocal;

			return centrePos_Snapped;
		}

		public static float SnapToGrid(float v)
		{
			int intV = Mathf.RoundToInt(v / GridSize);
			return intV * GridSize;
		}

		// Snap point to grid, with option to allow snapping to centre of grid cells (rather than just the grid lines)
		public static Vector2 SnapToGrid(Vector2 v, bool allowCentreSnapX = false, bool allowCentreSnapY = false)
		{
			int xM = allowCentreSnapX ? 2 : 1;
			int yM = allowCentreSnapY ? 2 : 1;

			return new Vector2(SnapToGrid(v.x * xM) / xM, SnapToGrid(v.y * yM) / yM);
		}

		public static float SnapToGridForceEven(float v)
		{
			int intV = Mathf.RoundToInt(v / GridSize);
			if ((intV & 1) != 0) intV++;
			return intV * GridSize;
		}

		public static Vector2 SnapToGridForceEven(Vector2 v) => new(SnapToGridForceEven(v.x), SnapToGridForceEven(v.y));

		public static Vector2 ForceStraightLine(Vector2 prev, Vector2 curr)
		{
			Vector2 offset = curr - prev;
			if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y)) offset.y = 0;
			else offset.x = 0;

			return prev + offset;
		}

		public static Vector2[] RouteWire(Vector2 prev, Vector2 curr)
		{
			Vector2[] points = new Vector2[2];
			Vector2 offset = curr - prev;

			if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
			{
				// Horizontal mode: move horizontally, then 45-degree diagonal to curr
				float diagLen = Mathf.Min(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
				float signX = Mathf.Sign(offset.x);
				float signY = Mathf.Sign(offset.y);
				Vector2 bend = new Vector2(curr.x - diagLen * signX, prev.y);
				points[0] = bend;
				points[1] = curr;
			}
			else
			{
				// Vertical mode: move vertically, then 45-degree diagonal to curr
				float diagLen = Mathf.Min(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
				float signX = Mathf.Sign(offset.x);
				float signY = Mathf.Sign(offset.y);
				Vector2 bend = new Vector2(prev.x, curr.y - diagLen * signY);
				points[0] = bend;
				points[1] = curr;
			}
			return points;
		}
	}
}