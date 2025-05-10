using DLS.Game;
using Seb.Helpers;
using UnityEngine;

namespace DLS.Graphics
{
	public static class WireLayoutHelper
	{
		public static void CreateMultiBitWireLayout(WireInstance.BitWire[] bitWires, WireInstance wire, float thickness)
		{
			// At 1, wires are spaced apart by their thickness. This can cause slight slivers to appear though due to antialiasing, so it helps to smoosh them together slightly 
			const float thicknessOffsetT = 0.925f;

			// Ensure initialized
			foreach (WireInstance.BitWire bitWire in bitWires)
			{
				if (bitWire.Points == null || bitWire.Points.Length != wire.WirePointCount)
				{
					bitWire.Points = new Vector2[wire.WirePointCount];
				}
			}

			Vector2 dirPrev = Vector2.zero;
			int numBits = bitWires.Length;
			float offsetSign = 1;

			// Create layout
			for (int i = 0; i < wire.WirePointCount - 1; i++)
			{
				Vector2 wireCentreA = wire.GetWirePoint(i);
				Vector2 wireCentreB = wire.GetWirePoint(i + 1);
				Vector2 wireDir = (wireCentreB - wireCentreA).normalized;
				Vector2 wirePerpDir = new(-wireDir.y, wireDir.x);

				// If wire bends back past a certain threshold, swap the offset direction
				// This gives appearance of wires flipping over, rather than bending at an uncomfortable angle, which I think looks better...
				if (i > 0) offsetSign *= Flip(wireDir, dirPrev);

				for (int bitIndex = 0; bitIndex < numBits; bitIndex++)
				{
					WireInstance.BitWire bitWire = bitWires[bitIndex];
					float bitOffsetDst = (bitIndex - (numBits - 1) / 2f) * thickness * 2 * thicknessOffsetT;

					Vector2 bitWireOffset = wirePerpDir * bitOffsetDst;
					Vector2 posA = i == 0 ? wireCentreA + bitWireOffset : bitWire.Points[i];
					Vector2 posB = wireCentreB + bitWireOffset * offsetSign;

					// If there is another point after this, position the wires to align with that direction
					if (i + 1 < wire.WirePointCount - 1)
					{
						Vector2 centreNext = wire.GetWirePoint(i + 2);
						if ((centreNext - wireCentreB).sqrMagnitude > 0.001f)
						{
							Vector2 dirNext = (centreNext - wireCentreB).normalized;
							Vector2 wireDirNext = new(-dirNext.y, dirNext.x);
							Vector2 bitWireOffsetNext = wireDirNext * bitOffsetDst;
							Vector2 posNext = centreNext + bitWireOffsetNext * (offsetSign * Flip(wireDir, dirNext));

							(bool intersects, Vector2 point) intersectResult = Maths.LineIntersectsLine(posA, posB, posNext, posNext + dirNext);

							if (Mathf.Abs(Vector2.Dot(wireDir, dirNext)) < 0.995f && intersectResult.intersects)
							{
								posB += intersectResult.point - posB;
							}
						}
					}

					bitWire.Points[i] = posA;
					bitWire.Points[i + 1] = posB;
				}

				dirPrev = wireDir;
			}
		}

		public static (Vector2 point, int segmentIndex) GetClosestPointOnWire(WireInstance wire, Vector2 desiredPos)
		{
			int bestSegmentIndex = 0;
			float bestSqrDst = float.MaxValue;
			Vector2 bestPoint = Vector2.zero;

			for (int i = 0; i < wire.WirePointCount - 1; i++)
			{
				Vector2 segStartPoint = wire.GetWirePoint(i);
				Vector2 segEndPoint = wire.GetWirePoint(i + 1);
				Vector2 pointOnSegment = Maths.ClosestPointOnLineSegment(desiredPos, segStartPoint, segEndPoint);

				float sqrDst = (pointOnSegment - desiredPos).sqrMagnitude;
				if (sqrDst < bestSqrDst)
				{
					bestPoint = pointOnSegment;
					bestSqrDst = sqrDst;
					bestSegmentIndex = i;
				}
			}

			return (bestPoint, bestSegmentIndex);
		}


		static float Flip(Vector2 dirA, Vector2 dirB)
		{
			// How far back to allow wire to be angled before switching sign
			// Without this, the miter would grow to infinity as wire angle approaches 180 degrees.
			// Instead, at a certain threshold we can switch from miter to 'flipping' the wire over.
			// TODO: maybe would be better to do something like insert additional point to allow for a 'cut corner' effect instead of miter, and then we could avoid this flip stuff
			const float threshold = -0.75f;
			return Vector2.Dot(dirA, dirB) < threshold ? -1 : 1;
		}
	}
}