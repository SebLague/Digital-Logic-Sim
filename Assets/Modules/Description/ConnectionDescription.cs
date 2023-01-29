namespace DLS.ChipData
{
	// The Connections structure defines the connections from a source pin to a target pin.
	// Source → Target describes the direction that information flows inside the simulation.
	// Put concretely, information flows from a chip’s input pins (sources) to the input pins of its subchips (targets).
	// Subsequently, the information flows from the output pins of the subchips (sources) to the output pins of the chip (targets).
	// So:
	// Sources = chip inputs / subchip outputs
	// Targets = chip outputs / subchip inputs
	public struct ConnectionDescription
	{
		public PinAddress Source;
		public PinAddress Target;
		public Point[] WirePoints;
		public string ColourThemeName;
	}
}