using System;
using DLS.Description;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Types;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Game
{
	public class DevPinInstance : IMoveable
	{
		public readonly PinBitCount BitCount;
		public readonly char[] decimalDisplayCharBuffer = new char[16];

		// Size/Layout info
		public readonly Vector2 faceDir;

		public readonly bool IsInputPin;
		public readonly string Name;
		public readonly PinInstance Pin;
		public readonly Vector2Int StateGridDimensions;
		public readonly Vector2 StateGridSize;

		public PinValueDisplayMode pinValueDisplayMode;

		public DevPinInstance(PinDescription pinDescription, bool isInput)
		{
			Name = pinDescription.Name;
			ID = pinDescription.ID;
			IsInputPin = isInput;
			Position = pinDescription.Position;
			BitCount = pinDescription.BitCount;

			Pin = new PinInstance(pinDescription, new PinAddress(ID, 0), this, isInput);
			pinValueDisplayMode = pinDescription.ValueDisplayMode;

			// Calculate layout info
			faceDir = new Vector2(IsInputPin ? 1 : -1, 0);
			StateGridDimensions = BitCount switch
			{
				PinBitCount.Bit1 => new Vector2Int(1, 1),
				PinBitCount.Bit4 => new Vector2Int(2, 2),
				PinBitCount.Bit8 => new Vector2Int(4, 2),
				_ => throw new Exception("Bit count not implemented")
			};
			StateGridSize = BitCount switch
			{
				PinBitCount.Bit1 => Vector2.one * (DevPinStateDisplayRadius * 2 + DevPinStateDisplayOutline * 2),
				_ => (Vector2)StateGridDimensions * MultiBitPinStateDisplaySquareSize + Vector2.one * DevPinStateDisplayOutline
			};
		}

		public Vector2 HandlePosition => Position;
		public Vector2 StateDisplayPosition => HandlePosition + faceDir * (DevPinHandleWidth / 2 + StateGridSize.x / 2 + 0.065f);

		public Vector2 PinPosition
		{
			get
			{
				int gridDst = BitCount is PinBitCount.Bit1 or PinBitCount.Bit4 ? 6 : 9;
				return HandlePosition + faceDir * (GridSize * gridDst);
			}
		}


		public Vector2 Position { get; set; }
		public Vector2 MoveStartPosition { get; set; }
		public Vector2 StraightLineReferencePoint { get; set; }
		public int ID { get; }

		public bool IsSelected { get; set; }
		public bool HasReferencePointForStraightLineMovement { get; set; }
		public bool IsValidMovePos { get; set; }

		public Bounds2D SelectionBoundingBox => CreateBoundingBox(SelectionBoundsPadding);

		public Bounds2D BoundingBox => CreateBoundingBox(0);


		public Vector2 SnapPoint => Pin.GetWorldPos();

		public bool ShouldBeIncludedInSelectionBox(Vector2 selectionCentre, Vector2 selectionSize)
		{
			Bounds2D selfBounds = SelectionBoundingBox;
			return Maths.BoxesOverlap(selectionCentre, selectionSize, selfBounds.Centre, selfBounds.Size);
		}

		public int GetStateDecimalDisplayValue()
		{
			uint rawValue = PinState.GetBitStates(Pin.State);
			int displayValue = (int)rawValue;

			if (pinValueDisplayMode == PinValueDisplayMode.SignedDecimal)
			{
				displayValue = Maths.TwosComplement(rawValue, (int)BitCount);
			}

			return displayValue;
		}

		Bounds2D CreateBoundingBox(float pad)
		{
			float x1 = HandlePosition.x - faceDir.x * DevPinHandleWidth / 2;
			float x2 = PinPosition.x + faceDir.x * PinRadius;
			float minX = Mathf.Min(x1, x2);
			float maxX = Mathf.Max(x1, x2);

			Vector2 centre = new((minX + maxX) / 2, HandlePosition.y);
			Vector2 size = new Vector2(maxX - minX, BoundsHeight()) + Vector2.one * pad;
			return Bounds2D.CreateFromCentreAndSize(centre, size);
		}

		public Bounds2D HandleBounds() => Bounds2D.CreateFromCentreAndSize(HandlePosition, GetHandleSize());

		public float BoundsHeight() => StateGridSize.y;

		public Vector2 GetHandleSize() => new(DevPinHandleWidth, BoundsHeight());

		public void ToggleState(int bitIndex)
		{
			PinState.Toggle(ref Pin.PlayerInputState, bitIndex);
		}

		public bool PointIsInInteractionBounds(Vector2 point) => PointIsInHandleBounds(point) || PointIsInStateIndicatorBounds(point);

		public bool PointIsInStateIndicatorBounds(Vector2 point) => Maths.PointInCircle2D(point, StateDisplayPosition, DevPinStateDisplayRadius);

		public bool PointIsInHandleBounds(Vector2 point) => HandleBounds().PointInBounds(point);
	}
}