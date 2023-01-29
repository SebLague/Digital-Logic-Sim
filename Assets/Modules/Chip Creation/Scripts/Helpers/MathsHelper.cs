using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public static class MathsHelper
	{
		public static (bool intersects, Vector2 intersectionPoint) LineIntersectsLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			float d = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
			if (d == 0) // parallel
			{
				return (false, Vector2.zero);
			}

			float n = (a1.x - b1.x) * (b1.y - b2.y) - (a1.y - b1.y) * (b1.x - b2.x);
			float t = n / d;
			Vector2 intersectionPoint = a1 + (a2 - a1) * t;
			return (true, intersectionPoint);
		}
	}
}