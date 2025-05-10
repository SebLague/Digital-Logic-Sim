using UnityEngine;

namespace DLS.Description
{
	public struct DisplayDescription
	{
		// ID of the subchip that is to be displayed on the case of this chip
		// (note: if -1, then this is a builtin chip display)
		public int SubChipID;
		public Vector2 Position;
		public float Scale;

		public DisplayDescription(int subChipID, Vector2 position, float scale)
		{
			SubChipID = subChipID;
			Position = position;
			Scale = scale;
		}
	}
}