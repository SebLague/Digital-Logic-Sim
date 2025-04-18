using System;
using System.Collections.Generic;
using DLS.Description;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Game
{
	public static class BuiltinChipCreator
	{
		static readonly Color ChipCol_SplitMerge = new(0.1f, 0.1f, 0.1f); //new(0.8f, 0.8f, 0.8f);

		public static ChipDescription[] CreateAllBuiltinChipDescriptions()
		{
			return new[]
			{
				// ---- I/O Pins ----
				CreateInputOrOutputPin(ChipType.In_1Bit),
				CreateInputOrOutputPin(ChipType.Out_1Bit),
				CreateInputOrOutputPin(ChipType.In_4Bit),
				CreateInputOrOutputPin(ChipType.Out_4Bit),
				CreateInputOrOutputPin(ChipType.In_8Bit),
				CreateInputOrOutputPin(ChipType.Out_8Bit),
				CreateInputOrOutputPin(ChipType.In_16Bit),
				CreateInputOrOutputPin(ChipType.Out_16Bit),
				CreateInputOrOutputPin(ChipType.In_32Bit),
				CreateInputOrOutputPin(ChipType.Out_32Bit),
				CreateInputOrOutputPin(ChipType.In_64Bit),
				CreateInputOrOutputPin(ChipType.Out_64Bit),
				CreateInputOrOutputPin(ChipType.In_128Bit),
				CreateInputOrOutputPin(ChipType.Out_128Bit),
				CreateInputOrOutputPin(ChipType.In_256Bit),
				CreateInputOrOutputPin(ChipType.Out_256Bit),
				

				CreateInputKeyChip(),
				// ---- Basic Chips ----
				CreateNand(),
				CreateTristateBuffer(),
				CreateClock(),
				// ---- Memory ----
				dev_CreateRAM_8(),
				CreateROM_8(),
				// ---- Merge / Split ----
				CreateBitConversionChip(ChipType.Split_4To1Bit, PinBitCount.Bit4, PinBitCount.Bit1, 1, 4},
				CreateBitConversionChip(ChipType.Split_8To1Bit, PinBitCount.Bit8, PinBitCount.Bit1, 1, 8},
				CreateBitConversionChip(ChipType.Split_16To1Bit, PinBitCount.Bit16, PinBitCount.Bit1, 1, 16},
				CreateBitConversionChip(ChipType.Split_32To1Bit, PinBitCount.Bit32, PinBitCount.Bit1, 1, 32},
				CreateBitConversionChip(ChipType.Split_64To1Bit, PinBitCount.Bit64, PinBitCount.Bit1, 1, 64},
				CreateBitConversionChip(ChipType.Split_128To1Bit, PinBitCount.Bit128, PinBitCount.Bit1, 1, 128},
				CreateBitConversionChip(ChipType.Split_256To1Bit, PinBitCount.Bit256, PinBitCount.Bit1, 1, 256},
				CreateBitConversionChip(ChipType.Split_8To4Bit, PinBitCount.Bit8, PinBitCount.Bit4, 1, 2},
				CreateBitConversionChip(ChipType.Split_16To4Bit, PinBitCount.Bit16, PinBitCount.Bit4, 1, 4},
				CreateBitConversionChip(ChipType.Split_32To4Bit, PinBitCount.Bit32, PinBitCount.Bit4, 1, 8},
				CreateBitConversionChip(ChipType.Split_64To4Bit, PinBitCount.Bit64, PinBitCount.Bit4, 1, 16},
				CreateBitConversionChip(ChipType.Split_128To4Bit, PinBitCount.Bit128, PinBitCount.Bit4, 1, 32},
				CreateBitConversionChip(ChipType.Split_256To4Bit, PinBitCount.Bit256, PinBitCount.Bit4, 1, 64},
				CreateBitConversionChip(ChipType.Split_16To8Bit, PinBitCount.Bit16, PinBitCount.Bit8, 1, 2},
				CreateBitConversionChip(ChipType.Split_32To8Bit, PinBitCount.Bit32, PinBitCount.Bit8, 1, 4},
				CreateBitConversionChip(ChipType.Split_64To8Bit, PinBitCount.Bit64, PinBitCount.Bit8, 1, 8},
				CreateBitConversionChip(ChipType.Split_128To8Bit, PinBitCount.Bit128, PinBitCount.Bit8, 1, 16},
				CreateBitConversionChip(ChipType.Split_256To8Bit, PinBitCount.Bit256, PinBitCount.Bit8, 1, 32},
				CreateBitConversionChip(ChipType.Split_32To16Bit, PinBitCount.Bit32, PinBitCount.Bit16, 1, 2},
				CreateBitConversionChip(ChipType.Split_64To16Bit, PinBitCount.Bit64, PinBitCount.Bit16, 1, 4},
				CreateBitConversionChip(ChipType.Split_128To16Bit, PinBitCount.Bit128, PinBitCount.Bit16, 1, 8},
				CreateBitConversionChip(ChipType.Split_256To16Bit, PinBitCount.Bit256, PinBitCount.Bit16, 1, 16},
				CreateBitConversionChip(ChipType.Split_64To32Bit, PinBitCount.Bit64, PinBitCount.Bit32, 1, 2},
				CreateBitConversionChip(ChipType.Split_128To32Bit, PinBitCount.Bit128, PinBitCount.Bit32, 1, 4},
				CreateBitConversionChip(ChipType.Split_256To32Bit, PinBitCount.Bit256, PinBitCount.Bit32, 1, 8},
				CreateBitConversionChip(ChipType.Split_128To64Bit, PinBitCount.Bit128, PinBitCount.Bit64, 1, 2},
				CreateBitConversionChip(ChipType.Split_256To64Bit, PinBitCount.Bit256, PinBitCount.Bit64, 1, 4},
				CreateBitConversionChip(ChipType.Split_256To128Bit, PinBitCount.Bit256, PinBitCount.Bit128, 1, 2},
				CreateBitConversionChip(ChipType.Merge_128To256Bit, PinBitCount.Bit128, PinBitCount.Bit256, 2, 1},
				CreateBitConversionChip(ChipType.Merge_64To256Bit, PinBitCount.Bit64, PinBitCount.Bit256, 4, 1},
				CreateBitConversionChip(ChipType.Merge_64To128Bit, PinBitCount.Bit64, PinBitCount.Bit128, 2, 1},
				CreateBitConversionChip(ChipType.Merge_32To256Bit, PinBitCount.Bit32, PinBitCount.Bit256, 8, 1},
				CreateBitConversionChip(ChipType.Merge_32To128Bit, PinBitCount.Bit32, PinBitCount.Bit128, 4, 1},
				CreateBitConversionChip(ChipType.Merge_32To64Bit, PinBitCount.Bit32, PinBitCount.Bit64, 2, 1},
				CreateBitConversionChip(ChipType.Merge_16To256Bit, PinBitCount.Bit16, PinBitCount.Bit256, 16, 1},
				CreateBitConversionChip(ChipType.Merge_16To128Bit, PinBitCount.Bit16, PinBitCount.Bit128, 8, 1},
				CreateBitConversionChip(ChipType.Merge_16To64Bit, PinBitCount.Bit16, PinBitCount.Bit64, 4, 1},
				CreateBitConversionChip(ChipType.Merge_16To32Bit, PinBitCount.Bit16, PinBitCount.Bit32, 2, 1},
				CreateBitConversionChip(ChipType.Merge_8To256Bit, PinBitCount.Bit8, PinBitCount.Bit256, 32, 1},
				CreateBitConversionChip(ChipType.Merge_8To128Bit, PinBitCount.Bit8, PinBitCount.Bit128, 16, 1},
				CreateBitConversionChip(ChipType.Merge_8To64Bit, PinBitCount.Bit8, PinBitCount.Bit64, 8, 1},
				CreateBitConversionChip(ChipType.Merge_8To32Bit, PinBitCount.Bit8, PinBitCount.Bit32, 4, 1},
				CreateBitConversionChip(ChipType.Merge_8To16Bit, PinBitCount.Bit8, PinBitCount.Bit16, 2, 1},
				CreateBitConversionChip(ChipType.Merge_4To256Bit, PinBitCount.Bit4, PinBitCount.Bit256, 64, 1},
				CreateBitConversionChip(ChipType.Merge_4To128Bit, PinBitCount.Bit4, PinBitCount.Bit128, 32, 1},
				CreateBitConversionChip(ChipType.Merge_4To64Bit, PinBitCount.Bit4, PinBitCount.Bit64, 16, 1},
				CreateBitConversionChip(ChipType.Merge_4To32Bit, PinBitCount.Bit4, PinBitCount.Bit32, 8, 1},
				CreateBitConversionChip(ChipType.Merge_4To16Bit, PinBitCount.Bit4, PinBitCount.Bit16, 4, 1},
				CreateBitConversionChip(ChipType.Merge_4To8Bit, PinBitCount.Bit4, PinBitCount.Bit8, 2, 1},
				CreateBitConversionChip(ChipType.Merge_1To256Bit, PinBitCount.Bit1, PinBitCount.Bit256, 256, 1},
				CreateBitConversionChip(ChipType.Merge_1To128Bit, PinBitCount.Bit1, PinBitCount.Bit128, 128, 1},
				CreateBitConversionChip(ChipType.Merge_1To64Bit, PinBitCount.Bit1, PinBitCount.Bit64, 64, 1},
				CreateBitConversionChip(ChipType.Merge_1To32Bit, PinBitCount.Bit1, PinBitCount.Bit32, 32, 1},
				CreateBitConversionChip(ChipType.Merge_1To16Bit, PinBitCount.Bit1, PinBitCount.Bit16, 16, 1},
				CreateBitConversionChip(ChipType.Merge_1To8Bit, PinBitCount.Bit1, PinBitCount.Bit8, 8, 1},
				CreateBitConversionChip(ChipType.Merge_1To4Bit, PinBitCount.Bit1, PinBitCount.Bit4, 4, 1},
				// ---- Displays ----
				CreateDisplay7Seg(),
				CreateDisplayRGB(),
				CreateDisplayDot(),
				CreateDisplayLED(),
				// ---- Bus ----
				CreateBus(PinBitCount.Bit1),
				CreateBus(PinBitCount.Bit4),
				CreateBus(PinBitCount.Bit8),
				CreateBus(PinBitCount.Bit16),
				CreateBus(PinBitCount.Bit32),
				CreateBus(PinBitCount.Bit64),
				CreateBus(PinBitCount.Bit128),
				CreateBus(PinBitCount.Bit256),
				CreateBusTerminus(PinBitCount.Bit1),
				CreateBusTerminus(PinBitCount.Bit4),
				CreateBusTerminus(PinBitCount.Bit8),
				CreateBusTerminus(PinBitCount.Bit16),
				CreateBusTerminus(PinBitCount.Bit32),
				CreateBusTerminus(PinBitCount.Bit64),
				CreateBusTerminus(PinBitCount.Bit128),
				CreateBusTerminus(PinBitCount.Bit256)
			};
		}

		static ChipDescription CreateNand()
		{
			Color col = new(0.73f, 0.26f, 0.26f);
			Vector2 size = new(CalculateGridSnappedWidth(GridSize * 8), GridSize * 4);

			PinDescription[] inputPins = { CreatePinDescription("IN B", 0), CreatePinDescription("IN A", 1) };
			PinDescription[] outputPins = { CreatePinDescription("OUT", 2) };

			return CreateBuiltinChipDescription(ChipType.Nand, size, col, inputPins, outputPins);
		}

		static ChipDescription dev_CreateRAM_8()
		{
			Color col = new(0.85f, 0.45f, 0.3f);

			PinDescription[] inputPins =
			{
				CreatePinDescription("ADDRESS", 0, PinBitCount.Bit8),
				CreatePinDescription("DATA", 1, PinBitCount.Bit8),
				CreatePinDescription("WRITE", 2),
				CreatePinDescription("RESET", 3),
				CreatePinDescription("CLOCK", 4)
			};
			PinDescription[] outputPins = { CreatePinDescription("OUT", 5, PinBitCount.Bit8) };
			Vector2 size = new(GridSize * 10, SubChipInstance.MinChipHeightForPins(inputPins, outputPins));

			return CreateBuiltinChipDescription(ChipType.dev_Ram_8Bit, size, col, inputPins, outputPins);
		}

		static ChipDescription CreateROM_8()
		{
			PinDescription[] inputPins =
			{
				CreatePinDescription("ADDRESS", 0, PinBitCount.Bit8)
			};
			PinDescription[] outputPins =
			{
				CreatePinDescription("OUT B", 1, PinBitCount.Bit8),
				CreatePinDescription("OUT A", 2, PinBitCount.Bit8)
			};

			Color col = new(0.25f, 0.35f, 0.5f);
			Vector2 size = new(GridSize * 12, SubChipInstance.MinChipHeightForPins(inputPins, outputPins));

			return CreateBuiltinChipDescription(ChipType.Rom_256x16, size, col, inputPins, outputPins);
		}

		static ChipDescription CreateInputKeyChip()
		{
			Color col = new(0.1f, 0.1f, 0.1f);
			Vector2 size = new Vector2(GridSize, GridSize) * 3;

			PinDescription[] outputPins = { CreatePinDescription("OUT", 0) };

			return CreateBuiltinChipDescription(ChipType.Key, size, col, null, outputPins, hideName: true);
		}


		static ChipDescription CreateTristateBuffer()
		{
			Color col = new(0.1f, 0.1f, 0.1f);
			Vector2 size = new(CalculateGridSnappedWidth(1.5f), GridSize * 5);

			PinDescription[] inputPins = { CreatePinDescription("IN", 0), CreatePinDescription("ENABLE", 1) };
			PinDescription[] outputPins = { CreatePinDescription("OUT", 2) };

			return CreateBuiltinChipDescription(ChipType.TriStateBuffer, size, col, inputPins, outputPins);
		}

		static ChipDescription CreateClock()
		{
			Vector2 size = new(GridHelper.SnapToGrid(1), GridSize * 3);
			Color col = new(0.1f, 0.1f, 0.1f);
			PinDescription[] outputPins = { CreatePinDescription("CLK", 0) };

			return CreateBuiltinChipDescription(ChipType.Clock, size, col, null, outputPins);
		}

		static ChipDescription CreateBitConversionChip(ChipType chipType, PinBitCount bitCountIn, PinBitCount bitCountOut, int numIn, int numOut)
		{
			PinDescription[] inputPins = new PinDescription[numIn];
			PinDescription[] outputPins = new PinDescription[numOut];

			for (int i = 0; i < numIn; i++)
			{
				string pinName = GetPinName(i, numIn, true);
				inputPins[i] = CreatePinDescription(pinName, i, bitCountIn);
			}

			for (int i = 0; i < numOut; i++)
			{
				string pinName = GetPinName(i, numOut, false);
				outputPins[i] = CreatePinDescription(pinName, numIn + i, bitCountOut);
			}

			float height = SubChipInstance.MinChipHeightForPins(inputPins, outputPins);
			Vector2 size = new(GridSize * 9, height);

			return CreateBuiltinChipDescription(chipType, size, ChipCol_SplitMerge, inputPins, outputPins);
		}

		static string GetPinName(int pinIndex, int pinCount, bool isInput)
		{
			string letter = " " + (char)('A' + pinCount - pinIndex - 1);
			if (pinCount == 1) letter = "";
			return (isInput ? "IN" : "OUT") + letter;
		}

		static ChipDescription CreateDisplay7Seg()
		{
			PinDescription[] inputPins =
			{
				CreatePinDescription("A", 0),
				CreatePinDescription("B", 1),
				CreatePinDescription("C", 2),
				CreatePinDescription("D", 3),
				CreatePinDescription("E", 4),
				CreatePinDescription("F", 5),
				CreatePinDescription("G", 6),
				CreatePinDescription("COL", 7)
			};

			Color col = new(0.1f, 0.1f, 0.1f);
			float height = SubChipInstance.MinChipHeightForPins(inputPins, null);
			Vector2 size = new(GridSize * 10, height);
			float displayWidth = size.x - GridSize * 2;

			DisplayDescription[] displays =
			{
				new()
				{
					Position = Vector2.right * PinRadius / 3 * 0,
					Scale = displayWidth,
					SubChipID = -1
				}
			};
			return CreateBuiltinChipDescription(ChipType.SevenSegmentDisplay, size, col, inputPins, null, displays, true);
		}

		static ChipDescription CreateDisplayRGB()
		{
			float height = GridSize * 21;
			float width = height;
			float displayWidth = height - GridSize * 2;

			Color col = new(0.1f, 0.1f, 0.1f);
			Vector2 size = new(width, height);

			PinDescription[] inputPins =
			{
				CreatePinDescription("ADDRESS", 0, PinBitCount.Bit8),
				CreatePinDescription("RED", 1, PinBitCount.Bit4),
				CreatePinDescription("GREEN", 2, PinBitCount.Bit4),
				CreatePinDescription("BLUE", 3, PinBitCount.Bit4),
				CreatePinDescription("RESET", 4),
				CreatePinDescription("WRITE", 5),
				CreatePinDescription("REFRESH", 6),
				CreatePinDescription("CLOCK", 7)
			};

			PinDescription[] outputPins =
			{
				CreatePinDescription("R OUT", 8, PinBitCount.Bit4),
				CreatePinDescription("G OUT", 9, PinBitCount.Bit4),
				CreatePinDescription("B OUT", 10, PinBitCount.Bit4)
			};

			DisplayDescription[] displays =
			{
				new()
				{
					Position = Vector2.right * PinRadius / 3 * 0,
					Scale = displayWidth,
					SubChipID = -1
				}
			};

			return CreateBuiltinChipDescription(ChipType.DisplayRGB, size, col, inputPins, outputPins, displays, true);
		}

		static ChipDescription CreateDisplayDot()
		{
			PinDescription[] inputPins =
			{
				CreatePinDescription("ADDRESS", 0, PinBitCount.Bit8),
				CreatePinDescription("PIXEL IN", 1),
				CreatePinDescription("RESET", 2),
				CreatePinDescription("WRITE", 3),
				CreatePinDescription("REFRESH", 4),
				CreatePinDescription("CLOCK", 5)
			};

			PinDescription[] outputPins =
			{
				CreatePinDescription("PIXEL OUT", 6)
			};

			float height = SubChipInstance.MinChipHeightForPins(inputPins, null);
			float width = height;
			float displayWidth = height - GridSize * 2;

			Color col = new(0.1f, 0.1f, 0.1f);
			Vector2 size = new(width, height);


			DisplayDescription[] displays =
			{
				new()
				{
					Position = Vector2.right * PinRadius / 3 * 0,
					Scale = displayWidth,
					SubChipID = -1
				}
			};

			return CreateBuiltinChipDescription(ChipType.DisplayDot, size, col, inputPins, outputPins, displays, true);
		}

		// (Not a chip, but convenient to treat it as one)
		public static ChipDescription CreateInputOrOutputPin(ChipType type)
		{
			(bool isInput, bool isOutput, PinBitCount numBits) = ChipTypeHelper.IsInputOrOutputPin(type);
			string name = isInput ? "IN" : "OUT";
			PinDescription[] pin = { CreatePinDescription(name, 0, numBits) };

			PinDescription[] inputs = isInput ? pin : null;
			PinDescription[] outputs = isOutput ? pin : null;

			return CreateBuiltinChipDescription(type, Vector2.zero, Color.clear, inputs, outputs);
		}

		static Vector2 BusChipSize(PinBitCount bitCount)
		{
			return bitCount switch
			{
				PinBitCount.Bit1 => new Vector2(GridSize * 2, GridSize * 2),
				PinBitCount.Bit4 => new Vector2(GridSize * 2, GridSize * 3),
				PinBitCount.Bit8 => new Vector2(GridSize * 2, GridSize * 4),
				PinBitCount.Bit16 => new Vector2(GridSize * 2, GridSize * 5),
				PinBitCount.Bit32 => new Vector2(GridSize * 2, GridSize * 6),
				PinBitCount.Bit64 => new Vector2(GridSize * 2, GridSize * 7),
				PinBitCount.Bit128 => new Vector2(GridSize * 2, GridSize * 8),
				PinBitCount.Bit256 => new Vector2(GridSize * 2, GridSize * 9),
				_ => throw new Exception("Bus bit count not implemented")
			};
		}

		static ChipDescription CreateBus(PinBitCount bitCount)
		{
			ChipType type = bitCount switch
			{
				PinBitCount.Bit1 => ChipType.Bus_1Bit,
				PinBitCount.Bit4 => ChipType.Bus_4Bit,
				PinBitCount.Bit8 => ChipType.Bus_8Bit,
				PinBitCount.Bit16 => ChipType.Bus_16Bit,
				PinBitCount.Bit32 => ChipType.Bus_32Bit,
				PinBitCount.Bit64 => ChipType.Bus_64Bit,
				PinBitCount.Bit128 => ChipType.Bus_128Bit,
				PinBitCount.Bit256 => ChipType.Bus_256Bit,
				_ => throw new Exception("Bus bit count not implemented")
			};

			string name = ChipTypeHelper.GetName(type);

			PinDescription[] inputs = { CreatePinDescription(name + " (Hidden)", 0, bitCount) };
			PinDescription[] outputs = { CreatePinDescription(name, 1, bitCount) };

			Color col = new(0.1f, 0.1f, 0.1f);

			return CreateBuiltinChipDescription(type, BusChipSize(bitCount), col, inputs, outputs, hideName: true);
		}

		static ChipDescription CreateDisplayLED()
		{
			PinDescription[] inputPins =
			{
				CreatePinDescription("IN", 0)
			};

			float height = SubChipInstance.MinChipHeightForPins(inputPins, null);
			float width = height;
			float displayWidth = height - GridSize * 0.5f;

			Color col = new(0.1f, 0.1f, 0.1f);
			Vector2 size = new(width, height);


			DisplayDescription[] displays =
			{
				new()
				{
					Position = Vector2.right * PinRadius / 3 * 0,
					Scale = displayWidth,
					SubChipID = -1
				}
			};

			return CreateBuiltinChipDesciption(ChipType.DisplayLED, size, col, inputPins, null, displays, true);
		}




		static ChipDescription CreateBusTerminus(PinBitCount bitCount)
		{
			ChipType type = bitCount switch
			{
				PinBitCount.Bit1 => ChipType.BusTerminus_1Bit,
				PinBitCount.Bit4 => ChipType.BusTerminus_4Bit,
				PinBitCount.Bit8 => ChipType.BusTerminus_8Bit,
				PinBitCount.Bit16 => ChipType.BusTerminus_16Bit,
				PinBitCount.Bit32 => ChipType.BusTerminus_32Bit,
				PinBitCount.Bit64 => ChipType.BusTerminus_64Bit,
				PinBitCount.Bit128 => ChipType.BusTerminus_128Bit,
				PinBitCount.Bit256 => ChipType.BusTerminus_256Bit,
				_ => throw new Exception("Bus bit count not implemented")
			};

			ChipDescription busOrigin = CreateBus(bitCount);
			PinDescription[] inputs = { CreatePinDescription(busOrigin.Name, 0, bitCount) };

			return CreateBuiltinChipDescription(type, BusChipSize(bitCount), busOrigin.Colour, inputs, hideName: true);
		}

		
		static ChipDescription CreateBuiltinChipDescription(ChipType type, Vector2 size, Color col, PinDescription[] inputs = null, PinDescription[] outputs = null, DisplayDescription[] displays = null, bool hideName = false)
		{
			string name = ChipTypeHelper.GetName(type);
			ValidatePinIDs(inputs, outputs, name);

			return new ChipDescription
			{
				Name = name,
				NameLocation = hideName ? NameDisplayLocation.Hidden : NameDisplayLocation.Centre,
				Colour = col,
				Size = new Vector2(size.x, size.y),
				InputPins = inputs ?? Array.Empty<PinDescription>(),
				OutputPins = outputs ?? Array.Empty<PinDescription>(),
				SubChips = Array.Empty<SubChipDescription>(),
				Wires = Array.Empty<WireDescription>(),
				Displays = displays,
				ChipType = type
			};
		}

		static PinDescription CreatePinDescription(string name, int id, PinBitCount bitCount = PinBitCount.Bit1) =>
			new(
				name,
				id,
				Vector2.zero,
				bitCount,
				PinColour.Red,
				PinValueDisplayMode.Off
			);

		static float CalculateGridSnappedWidth(float desiredWidth) =>
			// Calculate width such that spacing between an input and output pin on chip will align with grid
			GridHelper.SnapToGridForceEven(desiredWidth) - (ChipOutlineWidth - 2 * SubChipPinInset);

		static void ValidatePinIDs(PinDescription[] inputs, PinDescription[] outputs, string chipName)
		{
			HashSet<int> pinIDs = new();

			AddPins(inputs);
			AddPins(outputs);
			return;

			void AddPins(PinDescription[] pins)
			{
				if (pins == null) return;
				foreach (PinDescription pin in pins)
				{
					if (!pinIDs.Add(pin.ID))
					{
						throw new Exception($"Pin has duplicate ID ({pin.ID}) in builtin chip: {chipName}");
					}
				}
			}
		}
	}
}
