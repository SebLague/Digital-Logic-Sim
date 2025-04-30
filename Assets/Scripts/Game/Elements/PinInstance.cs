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
		public uint State; // sim state
		public uint PlayerInputState; // dev input pins only
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
			Colour = desc.Colour;

			IsBusPin = parent is SubChipInstance subchip && subchip.IsBus;
			faceRight = isSourcePin;
			PinState.SetAllDisconnected(ref State);
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

		public Color GetStateCol(int bitIndex, bool hover = false, bool canUsePlayerState = true)
		{
			uint pinState = (IsSourcePin && canUsePlayerState) ? PlayerInputState : State; // dev input pin uses player state (so it updates even when sim is paused)
			uint state = PinState.GetBitTristatedValue(pinState, bitIndex);

			if (state == PinState.LogicDisconnected) return DrawSettings.ActiveTheme.StateDisconnectedCol;
			return DrawSettings.GetStateColour(state == PinState.LogicHigh, (uint)Colour, hover);
			
		}
	}
}