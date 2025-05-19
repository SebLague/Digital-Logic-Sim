using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using Seb.Helpers;
using Seb.Types;
using UnityEngine;
using static DLS.Game.SubChipHelper;

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

			return;

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