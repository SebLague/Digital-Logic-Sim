using Newtonsoft.Json;

namespace DLS.ChipData
{
	public enum PinType { Unassigned, ChipInputPin, ChipOutputPin, SubChipInputPin, SubChipOutputPin }

	// Specifies a particular pin inside of a specific chip.
	// This could be one of the chip’s own input/output pins, or an input/output pin of one of its direct subchips.
	public struct PinAddress
	{
		public readonly PinType PinType;
		public readonly int SubChipID;
		public readonly int PinID;



		// Constructor
		public PinAddress(int subChipID, int pinID, PinType pinType)
		{
			this.SubChipID = subChipID;
			this.PinType = pinType;
			this.PinID = pinID;
		}

		// ---- Helpers ----
		[JsonIgnore]
		public bool BelongsToSubChip => PinType is PinType.SubChipInputPin or PinType.SubChipOutputPin;

		[JsonIgnore]
		public bool IsInputPin => PinType is PinType.ChipInputPin or PinType.SubChipInputPin;

		public static bool AreSame(PinAddress a, PinAddress b)
		{
			return a.SubChipID == b.SubChipID && a.PinID == b.PinID && a.PinType == b.PinType;
		}

		public override string ToString()
		{
			return $"SubChipID: {SubChipID} PinID: {PinID} PinType: {PinType}";
		}
	}
}