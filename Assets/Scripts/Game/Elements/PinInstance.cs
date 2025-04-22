using System;
using DLS.Description;
using DLS.Graphics;
using DLS.Simulation;
using UnityEngine;

namespace DLS.Game
{
	public class PinInstance : IInteractable
	{
		public readonly PinAddress Address;

		public readonly PinBitCount bitCount;
		public readonly bool IsBusPin;
		public readonly bool IsSourcePin;

		// Pin may be attached to a chip or a devPin as its parent
		public readonly IMoveable parent;
		public readonly PinState State;
		public PinColour Colour;
		bool faceRight;
		public float LocalPosY;
		public string Name;


		public PinInstance(PinDescription desc, PinAddress address, IMoveable parent, bool isSourcePin)
		{
			this.parent = parent;
			bitCount = desc.BitCount;
			Name = desc.Name;
			Address = address;
			IsSourcePin = isSourcePin;
			State = new PinState((int)bitCount);
			Colour = desc.Colour;

			IsBusPin = parent is SubChipInstance subchip && subchip.IsBus;
			faceRight = isSourcePin;
		}

		public Vector2 ForwardDir => faceRight ? Vector2.right : Vector2.left;


		public Vector2 GetWorldPos()
		{
			switch (parent)
			{
				case DevPinInstance devPin:
					return devPin.PinPosition;
				case SubChipInstance subchip:
				{
					Vector2 chipSize = subchip.Size;
					Vector2 chipPos = subchip.Position;

					float xLocal = (chipSize.x / 2 + DrawSettings.ChipOutlineWidth / 2 - DrawSettings.SubChipPinInset) * (faceRight ? 1 : -1);
					return chipPos + new Vector2(xLocal, LocalPosY);
				}
				default:
					throw new Exception("Parent type not supported");
			}
		}

		public void SetBusFlip(bool flipped)
		{
			faceRight = IsSourcePin ^ flipped;
		}

		public Color GetColLow() => DrawSettings.ActiveTheme.StateLowCol[(int)Colour];
		public Color GetColHigh() => DrawSettings.ActiveTheme.StateHighCol[(int)Colour];

		public Color GetStateCol(int bitIndex, bool hover = false)
		{
			uint state = State.GetBit(bitIndex);
			int colIndex = (int)Colour;

			return state switch
			{
				PinState.LogicHigh => DrawSettings.ActiveTheme.StateHighCol[colIndex],
				PinState.LogicLow => hover ? DrawSettings.ActiveTheme.StateHoverCol[colIndex] : DrawSettings.ActiveTheme.StateLowCol[colIndex],
				_ => DrawSettings.ActiveTheme.StateDisconnectedCol
			};
		}
	}
}