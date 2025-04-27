using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using Seb.Helpers;
using Seb.Types;
using UnityEngine;
using Exception = System.Exception;

namespace DLS.Game
{
	public class SubChipInstance : IMoveable
	{
		public readonly PinInstance[] AllPins;
		public readonly ChipType ChipType;
		public readonly ChipDescription Description;

		public readonly List<DisplayInstance> Displays;
		public readonly SubChipDescription InitialSubChipDesc;
		public readonly PinInstance[] InputPins;

		public readonly uint[] InternalData;
		public readonly bool IsBus;
		public readonly Vector2 MinSize;

		public readonly string MultiLineName;
		public readonly PinInstance[] OutputPins;
		public string activationKeyString; // input char for the 'key chip' type (stored as string to avoid allocating when drawing)
		public string Label;

		public SubChipInstance(ChipDescription description, SubChipDescription subChipDesc)
		{
			InitialSubChipDesc = subChipDesc;
			ChipType = description.ChipType;
			Description = description;
			Position = subChipDesc.Position;
			ID = subChipDesc.ID;
			Label = subChipDesc.Label;
			IsBus = ChipTypeHelper.IsBusType(ChipType);
			MultiLineName = CreateMultiLineName(description.Name);
			MinSize = CalculateMinChipSize(description.InputPins, description.OutputPins, description.Name);

			InputPins = CreatePinInstances(description.InputPins, true);
			OutputPins = CreatePinInstances(description.OutputPins, false);
			AllPins = InputPins.Concat(OutputPins).ToArray();
			LoadOutputPinColours(subChipDesc.OutputPinColourInfo);

			// Displays
			Displays = CreateDisplayInstances(description);

			// Load internal data (or create default in case missing)
			if (subChipDesc.InternalData == null || subChipDesc.InternalData.Length == 0)
			{
				InternalData = DescriptionCreator.CreateDefaultInstanceData(description.ChipType);
			}
			else
			{
				InternalData = new uint[subChipDesc.InternalData.Length];
				Array.Copy(subChipDesc.InternalData, InternalData, InternalData.Length);

				if (ChipType == ChipType.Key)
				{
					SetKeyChipActivationChar((char)subChipDesc.InternalData[0]);
				}

				if (IsBus && InternalData.Length > 1)
				{
					foreach (PinInstance pin in AllPins)
					{
						pin.SetBusFlip(BusIsFlipped);
					}
				}
			}

			PinInstance[] CreatePinInstances(PinDescription[] pinDescriptions, bool isInputPin)
			{
				int num = pinDescriptions.Length;
				PinInstance[] pins = new PinInstance[pinDescriptions.Length];

				for (int i = 0; i < num; i++)
				{
					PinDescription desc = pinDescriptions[i];
					PinAddress address = new(subChipDesc.ID, desc.ID);
					pins[i] = new PinInstance(desc, address, this, !isInputPin);
				}

				CalculatePinLayout(pins);

				return pins;
			}
		}

		public int LinkedBusPairID => IsBus ? (int)InternalData[0] : -1;
		public bool BusIsFlipped => IsBus && InternalData.Length > 1 && InternalData[1] == 1;
		public Vector2 Size => Description.Size;
		public Vector2 Position { get; set; }

		public Vector2 MoveStartPosition { get; set; }
		public Vector2 StraightLineReferencePoint { get; set; }
		public int ID { get; }

		public bool IsSelected { get; set; }
		public bool HasReferencePointForStraightLineMovement { get; set; }
		public bool IsValidMovePos { get; set; }

		public Bounds2D BoundingBox => CreateBoundingBox(0);
		public Bounds2D SelectionBoundingBox => CreateBoundingBox(DrawSettings.SelectionBoundsPadding);

		public Vector2 SnapPoint
		{
			get
			{
				if (InputPins.Length != 0) return InputPins[0].GetWorldPos();
				if (OutputPins.Length != 0) return OutputPins[0].GetWorldPos();
				return Position;
			}
		}

		public bool ShouldBeIncludedInSelectionBox(Vector2 selectionCentre, Vector2 selectionSize) => Maths.PointInBox2D(Position, selectionCentre, selectionSize);

		public void SetLinkedBusPair(SubChipInstance busPair)
		{
			if (!IsBus) throw new Exception("Can't link bus to non-bus chip");

			InternalData[0] = (uint)busPair.ID;
		}

		public void SetKeyChipActivationChar(char c)
		{
			if (ChipType != ChipType.Key) throw new Exception("Expected KeyChip type, but instead got: " + ChipType);
			activationKeyString = c.ToString();
			InternalData[0] = c;
		}

		public void UpdatePinLayout()
		{
			CalculatePinLayout(InputPins);
			CalculatePinLayout(OutputPins);
		}

		void CalculatePinLayout(PinInstance[] pins)
		{
			// If only one pin, it should be placed in the centre
			if (pins.Length == 1)
			{
				pins[0].LocalPosY = 0;
				return;
			}

			float chipTop = Size.y / 2;
			float startY = chipTop;

			(float chipHeight, float[] pinGridY) info = CalculateDefaultPinLayout(pins.Select(s => s.bitCount).ToArray());

			// ---- First pass: layout pins without any spacing between them ----
			for (int i = 0; i < pins.Length; i++)
			{
				PinInstance pin = pins[i];

				float pinGridY = info.pinGridY[i];
				pin.LocalPosY = startY + pinGridY * DrawSettings.GridSize;
			}


			// ---- Second pass: evenly distribute the remaining space between the pins ----
			float spaceRemaining = Size.y - info.chipHeight;

			if (spaceRemaining > 0)
			{
				float spacingBetweenPins = spaceRemaining / (pins.Length - 1);
				for (int i = 1; i < pins.Length; i++)
				{
					pins[i].LocalPosY -= spacingBetweenPins * i;
				}
			}
		}

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

		static List<DisplayInstance> CreateDisplayInstances(ChipDescription chipDesc)
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

		Bounds2D CreateBoundingBox(float pad)
		{
			float pinWidthPad = 0;
			float offsetX = 0;
			bool inputsHidden = ChipTypeHelper.IsBusOriginType(ChipType);
			float flipX = BusIsFlipped ? -1 : 1;

			if (InputPins.Length > 0 && !inputsHidden)
			{
				pinWidthPad += DrawSettings.PinRadius;
				offsetX -= DrawSettings.PinRadius / 2 * flipX;
			}

			if (OutputPins.Length > 0)
			{
				pinWidthPad += DrawSettings.PinRadius;
				offsetX += DrawSettings.PinRadius / 2 * flipX;
			}

			Vector2 padFinal = new(pinWidthPad + DrawSettings.ChipOutlineWidth + pad, DrawSettings.ChipOutlineWidth + pad);
			return Bounds2D.CreateFromCentreAndSize(Position + Vector2.right * offsetX, Size + padFinal);
		}

		public static Vector2 CalculateMinChipSize(PinDescription[] inputPins, PinDescription[] outputPins, string unformattedName)
		{
			float minHeightForPins = MinChipHeightForPins(inputPins, outputPins);
			string multiLineName = CreateMultiLineName(unformattedName);
			bool hasMultiLineName = multiLineName != unformattedName;
			float minNameHeight = DrawSettings.GridSize * (hasMultiLineName ? 4 : 3);

			Vector2 nameDrawBoundsSize = DevSceneDrawer.CalculateChipNameBounds(multiLineName);

			float sizeX = Mathf.Max(nameDrawBoundsSize.x + DrawSettings.GridSize, DrawSettings.PinRadius * 4);
			float sizeY = Mathf.Max(minNameHeight, minHeightForPins);

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

		// Split chip name into two lines (if contains a space character)
		static string CreateMultiLineName(string name)
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

		public void FlipBus()
		{
			if (!IsBus) throw new Exception("Can't flip non-bus type");

			bool isFlipped = !BusIsFlipped;
			InternalData[1] = isFlipped ? 1u : 0;

			foreach (PinInstance pin in AllPins)
			{
				pin.SetBusFlip(isFlipped);
			}
		}

		void LoadOutputPinColours(OutputPinColourInfo[] cols)
		{
			if (cols == null) return;

			foreach (PinInstance pin in OutputPins)
			{
				foreach (OutputPinColourInfo colInfo in cols)
				{
					if (colInfo.PinID == pin.Address.PinID)
					{
						pin.Colour = colInfo.PinColour;
					}
				}
			}
		}
	}
}