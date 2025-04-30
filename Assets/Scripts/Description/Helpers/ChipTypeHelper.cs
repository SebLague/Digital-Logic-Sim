using System;
using System.Collections.Generic;

namespace DLS.Description
{
	public static class ChipTypeHelper
	{
		const string mulSymbol = "\u00d7";

		static readonly Dictionary<ChipType, string> Names = new()
		{
			// ---- Basic Chips ----
			{ ChipType.Nand, "NAND" },
			{ ChipType.Clock, "CLOCK" },
			{ ChipType.Pulse, "PULSE" },
			{ ChipType.TriStateBuffer, "3-STATE BUFFER" },
			// ---- Memory ----
			{ ChipType.dev_Ram_8Bit, "dev.RAM-8" },
			{ ChipType.Rom_256x16, $"ROM 256{mulSymbol}16" },
			// ---- Split / Merge ----
			{ ChipType.Split_4To1Bit, "4-1BIT" },
			{ ChipType.Split_8To1Bit, "8-1BIT" },
			{ ChipType.Split_8To4Bit, "8-4BIT" },
			{ ChipType.Merge_4To8Bit, "4-8BIT" },
			{ ChipType.Merge_1To8Bit, "1-8BIT" },
			{ ChipType.Merge_1To4Bit, "1-4BIT" },

			// ---- Displays -----
			{ ChipType.DisplayRGB, "RGB DISPLAY" },
			{ ChipType.DisplayDot, "DOT DISPLAY" },
			{ ChipType.SevenSegmentDisplay, "7-SEGMENT" },
			{ ChipType.DisplayLED, "LED" },

			{ ChipType.Buzzer, "BUZZER" },

			// ---- Not really chips (but convenient to treat them as such anyway) ----

			// ---- Inputs/Outputs ----
			{ ChipType.In_1Bit, "IN-1" },
			{ ChipType.In_4Bit, "IN-4" },
			{ ChipType.In_8Bit, "IN-8" },
			{ ChipType.Out_1Bit, "OUT-1" },
			{ ChipType.Out_4Bit, "OUT-4" },
			{ ChipType.Out_8Bit, "OUT-8" },
			{ ChipType.Key, "KEY" },
			// ---- Buses ----
			{ ChipType.Bus_1Bit, "BUS-1" },
			{ ChipType.Bus_4Bit, "BUS-4" },
			{ ChipType.Bus_8Bit, "BUS-8" },
			{ ChipType.BusTerminus_1Bit, "BUS-TERMINUS-1" },
			{ ChipType.BusTerminus_4Bit, "BUS-TERMINUS-4" },
			{ ChipType.BusTerminus_8Bit, "BUS-TERMINUS-8" }
		};

		public static string GetName(ChipType type) => Names[type];

		public static bool IsBusType(ChipType type) => IsBusOriginType(type) || IsBusTerminusType(type);

		public static bool IsBusOriginType(ChipType type) => type is ChipType.Bus_1Bit or ChipType.Bus_4Bit or ChipType.Bus_8Bit;

		public static bool IsBusTerminusType(ChipType type) => type is ChipType.BusTerminus_1Bit or ChipType.BusTerminus_4Bit or ChipType.BusTerminus_8Bit;

		public static bool IsRomType(ChipType type) => type == ChipType.Rom_256x16;

		public static ChipType GetCorrespondingBusTerminusType(ChipType type)
		{
			return type switch
			{
				ChipType.Bus_1Bit => ChipType.BusTerminus_1Bit,
				ChipType.Bus_4Bit => ChipType.BusTerminus_4Bit,
				ChipType.Bus_8Bit => ChipType.BusTerminus_8Bit,
				_ => throw new Exception("No corresponding bus terminus found for type: " + type)
			};
		}

		public static ChipType GetPinType(bool isInput, PinBitCount numBits)
		{
			if (isInput)
			{
				return numBits switch
				{
					PinBitCount.Bit1 => ChipType.In_1Bit,
					PinBitCount.Bit4 => ChipType.In_4Bit,
					PinBitCount.Bit8 => ChipType.In_8Bit,
					_ => throw new Exception("No input pin type found for bitcount: " + numBits)
				};
			}

			return numBits switch
			{
				PinBitCount.Bit1 => ChipType.Out_1Bit,
				PinBitCount.Bit4 => ChipType.Out_4Bit,
				PinBitCount.Bit8 => ChipType.Out_8Bit,
				_ => throw new Exception("No output pin type found for bitcount: " + numBits)
			};
		}

		public static (bool isInput, bool isOutput, PinBitCount numBits) IsInputOrOutputPin(ChipType type)
		{
			return type switch
			{
				ChipType.In_1Bit => (true, false, PinBitCount.Bit1),
				ChipType.Out_1Bit => (false, true, PinBitCount.Bit1),
				ChipType.In_4Bit => (true, false, PinBitCount.Bit4),
				ChipType.Out_4Bit => (false, true, PinBitCount.Bit4),
				ChipType.In_8Bit => (true, false, PinBitCount.Bit8),
				ChipType.Out_8Bit => (false, true, PinBitCount.Bit8),
				_ => (false, false, PinBitCount.Bit1)
			};
		}
	}
}