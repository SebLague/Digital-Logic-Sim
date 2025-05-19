using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using Seb.Vis;
using UnityEngine;

namespace DLS.Game
{
	public static class SubChipHelper
	{
		// Min chip height based on input and output pins
		public static float MinChipHeightForPins(PinDescription[] inputs, PinDescription[] outputs) => Mathf.Max(MinChipHeightForPins(inputs), MinChipHeightForPins(outputs));

		public static float MinChipHeightForPins(PinDescription[] pins)
		{
			if (pins == null || pins.Length == 0) return 0;
			return CalculateDefaultPinLayout(pins.Select(p => p.BitCount).ToArray()).chipHeight;
		}

		// Calculate minimal height of chip to fit the given pins, and calculate their y positions (in grid space)
		public static (float chipHeight, float[] pinGridY) CalculateDefaultPinLayout(PinBitCount[] pins)
		{
			int gridY = 0; // top
			float[] pinGridYVals = new float[pins.Length];

			for (int i = 0; i < pins.Length; i++)
			{
				PinBitCount pinBitCount = pins[i];
				int pinGridHeight = pinBitCount switch
				{
					PinBitCount.Bit1 => 2,
					PinBitCount.Bit4 => 3,
					_ => 4
				};

				pinGridYVals[i] = gridY - pinGridHeight / 2f;
				gridY -= pinGridHeight;
			}

			float height = Mathf.Abs(gridY) * DrawSettings.GridSize;
			return (height, pinGridYVals);
		}


		public static Vector2 CalculateMinChipSize(ChipDescription desc)
		{
			return CalculateMinChipSize(desc.InputPins, desc.OutputPins, desc.Name, desc.NameLocation);
		}

		public static Vector2 CalculateMinChipSize(PinDescription[] inputPins, PinDescription[] outputPins, string unformattedName, NameDisplayLocation nameMode)
		{
			// Pin bounds
			float minHeightForPins = MinChipHeightForPins(inputPins, outputPins);
			bool hasInputs = inputPins?.Length > 0;
			bool hasOutputs = outputPins?.Length > 0;
			float minWidthForPins = (hasInputs || hasOutputs) ? DrawSettings.GridSize * 2 : 0;

			// Name bounds
			Vector2 nameDrawBoundsSize = Vector2.zero;
			if (nameMode != NameDisplayLocation.Hidden)
			{
				string multiLineName = CreateMultiLineName(unformattedName);
				nameDrawBoundsSize = DevSceneDrawer.CalculateChipNameBounds(multiLineName);
				nameDrawBoundsSize.y = Mathf.Max(nameDrawBoundsSize.y, DrawSettings.GridSize * 4);
			}

			// Final bounds
			float sizeX = Mathf.Max(Mathf.Max(nameDrawBoundsSize.x, minWidthForPins), DrawSettings.GridSize);
			float sizeY = Mathf.Max(Mathf.Max(nameDrawBoundsSize.y, minHeightForPins), DrawSettings.GridSize);

			return new Vector2(sizeX, sizeY);
		}


		public static float PinHeightFromBitCount(PinBitCount bitCount)
		{
			return bitCount switch
			{
				PinBitCount.Bit1 => DrawSettings.PinRadius * 2,
				PinBitCount.Bit4 => DrawSettings.PinHeight4Bit,
				PinBitCount.Bit8 => DrawSettings.PinHeight8Bit,
				_ => throw new Exception("Bit count not implemented " + bitCount)
			};
		}

		public static bool UseSingleLineName(string singleLineName, float width)
		{
			float textWidth = Draw.CalculateTextBoundsSize(singleLineName, DrawSettings.FontSizeChipName, DrawSettings.FontBold).x;
			return textWidth < width - DrawSettings.GridSize;
		}

		// Split chip name into two lines (if contains a space character)
		public static string CreateMultiLineName(string name)
		{
			// If name is short, or contains no spaces, then just keep on single line
			if (name.Length <= 6 || !name.Contains(' ')) return name;

			string[] lines = { name };
			float bestSplitPenalty = float.MaxValue;

			for (int i = 0; i < name.Length; i++)
			{
				if (name[i] == ' ')
				{
					string lineA = name.Substring(0, i).Trim();
					string lineB = name.Substring(i).Trim();
					int lenDiff = lineA.Length - lineB.Length;
					float splitPenalty = Mathf.Abs(lenDiff);
					if (splitPenalty < bestSplitPenalty)
					{
						lines = new[] { lineA, lineB };
						bestSplitPenalty = splitPenalty;
					}
				}
			}


			// Pad lines with spaces to centre justify
			string formatted = "";
			int longestLine = lines.Max(l => l.Length);

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				int numPadChars = longestLine - line.Length;
				int numPadLeft = numPadChars / 2;
				int numPadRight = numPadChars - numPadLeft;
				line = line.PadLeft(line.Length + numPadLeft, ' ');
				line = line.PadRight(line.Length + numPadRight, ' ');

				// Add half space tag to center if padding is uneven
				if (numPadLeft < numPadRight)
				{
					line = "<halfSpace>" + line;
				}

				formatted += line;
				if (i < lines.Length - 1) formatted += "\n";
			}

			return formatted;
		}

		public static List<DisplayInstance> CreateDisplayInstances(ChipDescription chipDesc)
		{
			List<DisplayInstance> list = new();
			if (chipDesc.HasDisplay())
			{
				foreach (DisplayDescription displayDesc in chipDesc.Displays)
				{
					try
					{
						list.Add(CreateDisplayInstance(displayDesc, chipDesc));
					}
					catch (Exception e)
					{
						Debug.Log("Failed to create display (this is expected if display has been deleted by player). Error: " + e.Message);
					}
				}
			}

			return list;
		}

		static DisplayInstance CreateDisplayInstance(DisplayDescription displayDesc, ChipDescription chipDesc)
		{
			DisplayInstance instance = new();
			instance.Desc = displayDesc;
			instance.DisplayType = chipDesc.ChipType;

			if (chipDesc.ChipType == ChipType.Custom)
			{
				ChipDescription childDesc = GetDescriptionOfDisplayedSubChip(chipDesc, displayDesc.SubChipID);
				instance.ChildDisplays = CreateDisplayInstances(childDesc);
			}


			return instance;
		}


		static ChipDescription GetDescriptionOfDisplayedSubChip(ChipDescription chipDesc, int subchipID)
		{
			ChipLibrary library = Project.ActiveProject.chipLibrary;

			foreach (SubChipDescription subchipDesc in chipDesc.SubChips)
			{
				if (subchipDesc.ID == subchipID)
				{
					return library.GetChipDescription(subchipDesc.Name);
				}
			}

			throw new Exception("Chip for display not found " + chipDesc.Name + " subchip id: " + subchipID);
		}
	}
}