namespace DLS.ChipData
{
	// Instance data for a SubChip (a chip contained within another chip).
	public struct ChipInstanceData
	{
		// Name of the subchip: used to look up its full ChipDescription
		public string Name;
		// Unique ID: used to identify a particular subchip (since subchips of the same kind will share the same name)
		public int ID;
		// Position of the subchip inside its parent chip
		// (This is an array because some specialized chips, such as a Bus chip for example, may be defined by multiple points).
		public Point[] Points;
		// Array of arbitrary data that could be used by some specialized chips, such as a ROM chip for example.
		public byte[] Data;
	}
}