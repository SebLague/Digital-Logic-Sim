namespace DLS.Description
{
	public enum ChipType
	{
		Custom,

		// ---- Basic Chips ----
		Nand,
		TriStateBuffer,
		Clock,
		Pulse,

		// ---- Memory ----
		dev_Ram_8Bit,
		Rom_256x16,

		// ---- Displays ----
		SevenSegmentDisplay,
		DisplayRGB,
		DisplayDot,
		DisplayLED,

		// ---- Merge / Split ----
		Merge_1To4Bit,
		Merge_1To8Bit,
		Merge_4To8Bit,
		Split_4To1Bit,
		Split_8To4Bit,
		Split_8To1Bit,

		// ---- In / Out Pins ----
		In_1Bit,
		In_4Bit,
		In_8Bit,
		Out_1Bit,
		Out_4Bit,
		Out_8Bit,

		Key,

		// ---- Buses ----
		Bus_1Bit,
		BusTerminus_1Bit,
		Bus_4Bit,
		BusTerminus_4Bit,
		Bus_8Bit,
		BusTerminus_8Bit,
		
		// ---- Audio ----
		Buzzer

	}
}