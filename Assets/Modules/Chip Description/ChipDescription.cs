namespace DLS.ChipData
{
	// Description of a chip: used for saving/loading data, and for setting up the simulation.
	public struct ChipDescription
	{
		// Name of the chip. This must be unique and not conflict with built-in chip names
		public string Name;
		// Colour to display this chip (e.g. "#FF0000")
		public string Colour;

		// Description of this chip's input and output pins (such as their display names and other settings)
		public PinDescription[] InputPins;
		public PinDescription[] OutputPins;

		// Description of all sub-chips (chips contained within this chip).
		// The description contains their names and their names and positions within the chip.
		public ChipInstanceData[] SubChips;

		// Description of all connections (wires) between this chip and its subChips.
		public ConnectionDescription[] Connections;

	}
}