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
				CreateInputKeyChip(),
				// ---- Basic Chips ----
				CreateNand(),
				CreateTristateBuffer(),
				CreateClock(),
				CreatePulse(),
				// ---- Memory ----
				dev_CreateRAM_8(),
				CreateROM_8(),
				// ---- Merge / Split ----
				CreateBitConversionChip(ChipType.Split_4To1Bit, PinBitCount.Bit4, PinBitCount.Bit1, 1, 4),
				CreateBitConversionChip(ChipType.Split_8To4Bit, PinBitCount.Bit8, PinBitCount.Bit4, 1, 2),
				CreateBitConversionChip(ChipType.Split_8To1Bit, PinBitCount.Bit8, PinBitCount.Bit1, 1, 8),

				CreateBitConversionChip(ChipType.Merge_1To8Bit, PinBitCount.Bit1, PinBitCount.Bit8, 8, 1),
				CreateBitConversionChip(ChipType.Merge_1To4Bit, PinBitCount.Bit1, PinBitCount.Bit4, 4, 1),
				CreateBitConversionChip(ChipType.Merge_4To8Bit, PinBitCount.Bit4, PinBitCount.Bit8, 2, 1),
				// ---- Displays ----
				CreateDisplay7Seg(),
				CreateDisplayRGB(),
				CreateDisplayDot(),
				CreateDisplayLED(),
				// ---- Bus ----
				CreateBus(PinBitCount.Bit1),
				CreateBusTerminus(PinBitCount.Bit1),
				CreateBus(PinBitCount.Bit4),
				CreateBusTerminus(PinBitCount.Bit4),
				CreateBus(PinBitCount.Bit8),
				CreateBusTerminus(PinBitCount.Bit8),
				// ---- Audio ----
				CreateBuzzer()
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

		static ChipDescription CreateBuzzer()
		{
			Color col = new(0, 0, 0);

			PinDescription[] inputPins =
			{
				CreatePinDescription("PITCH", 1, PinBitCount.Bit8),
				CreatePinDescription("VOLUME", 0, PinBitCount.Bit4),
			};

			float height = SubChipInstance.MinChipHeightForPins(inputPins, null);
			Vector2 size = new(CalculateGridSnappedWidth(GridSize * 9), height);

			return CreateBuiltinChipDescription(ChipType.Buzzer, size, col, inputPins, null, null);
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

			return CreateBuiltinChipDescription(ChipType.Key, size, col, null, outputPins, null, NameDisplayLocation.Hidden);
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

		static ChipDescription CreatePulse()
		{
			Vector2 size = new(GridHelper.SnapToGrid(1), GridSize * 3);
			Color col = new(0.1f, 0.1f, 0.1f);
			PinDescription[] inputPins = { CreatePinDescription("IN", 0) };
			PinDescription[] outputPins = { CreatePinDescription("PULSE", 1) };

			return CreateBuiltinChipDescription(ChipType.Pulse, size, col, inputPins, outputPins);
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
					Position = Vector2.zero,
					Scale = displayWidth,
					SubChipID = -1
				}
			};
			return CreateBuiltinChipDescription(ChipType.SevenSegmentDisplay, size, col, inputPins, null, displays, NameDisplayLocation.Hidden);
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
					Position = Vector2.zero,
					Scale = displayWidth,
					SubChipID = -1
				}
			};

			return CreateBuiltinChipDescription(ChipType.DisplayRGB, size, col, inputPins, outputPins, displays, NameDisplayLocation.Hidden);
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
					Position = Vector2.zero,
					Scale = displayWidth,
					SubChipID = -1
				}
			};

			return CreateBuiltinChipDescription(ChipType.DisplayDot, size, col, inputPins, outputPins, displays, NameDisplayLocation.Hidden);
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
				_ => throw new Exception("Bus bit count not implemented")
			};

			string name = ChipTypeHelper.GetName(type);

			PinDescription[] inputs = { CreatePinDescription(name + " (Hidden)", 0, bitCount) };
			PinDescription[] outputs = { CreatePinDescription(name, 1, bitCount) };

			Color col = new(0.1f, 0.1f, 0.1f);

			return CreateBuiltinChipDescription(type, BusChipSize(bitCount), col, inputs, outputs, null, NameDisplayLocation.Hidden);
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
					Position = Vector2.zero,
					Scale = displayWidth,
					SubChipID = -1
				}
			};

			return CreateBuiltinChipDescription(ChipType.DisplayLED, size, col, inputPins, null, displays, NameDisplayLocation.Hidden);
		}


		static ChipDescription CreateBusTerminus(PinBitCount bitCount)
		{
			ChipType type = bitCount switch
			{
				PinBitCount.Bit1 => ChipType.BusTerminus_1Bit,
				PinBitCount.Bit4 => ChipType.BusTerminus_4Bit,
				PinBitCount.Bit8 => ChipType.BusTerminus_8Bit,
				_ => throw new Exception("Bus bit count not implemented")
			};

			ChipDescription busOrigin = CreateBus(bitCount);
			PinDescription[] inputs = { CreatePinDescription(busOrigin.Name, 0, bitCount) };

			return CreateBuiltinChipDescription(type, BusChipSize(bitCount), busOrigin.Colour, inputs, null, null, NameDisplayLocation.Hidden);
		}


		static ChipDescription CreateBuiltinChipDescription(ChipType type, Vector2 size, Color col, PinDescription[] inputs, PinDescription[] outputs, DisplayDescription[] displays = null, NameDisplayLocation nameLoc = NameDisplayLocation.Centre)
		{
			string name = ChipTypeHelper.GetName(type);
			ValidatePinIDs(inputs, outputs, name);

			return new ChipDescription
			{
				Name = name,
				NameLocation = nameLoc,
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