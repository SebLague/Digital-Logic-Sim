using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using DLS.ChipData;
using System.Linq;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	// Handles the highlighting of pins and the connecting of wires between them
	public class WireEditor : ControllerBase
	{
		// ===== Public stuff =====
		// Note: this event is triggered once the wire has actually been connected, not when first instantiated during wire placement
		public event System.Action<Wire> WireCreated;
		// Note: this event is only triggered on wires that have been connected, not on wires deleted during placement
		public event System.Action<Wire> WireDeleted;

		public ReadOnlyCollection<Wire> AllWires => new(allConnectedWires);
		public bool IsCreatingWire => wireUnderConstruction != null;
		public override bool IsBusy() => IsCreatingWire;
		public Wire WireUnderMouse => wireUnderMouse;

		// ===== Inspector fields =====
		[SerializeField] Wire wirePrefab;
		[SerializeField] Transform wireHolder;

		// ===== Private fields =====
		List<Wire> allConnectedWires;
		HashSet<(PinType, PinType)> validConnectionsLookup;

		Pin wireStartPin;
		Wire wireStartWire;
		Wire wireUnderConstruction;
		Wire wireUnderMouse;

		bool creatingWireFromPin => IsCreatingWire && wireStartPin != null;
		bool creatingWireFromWire => IsCreatingWire && wireStartWire != null;


		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);

			allConnectedWires = new List<Wire>();
			InitValidConnectionLookup();

			editor.ChipPlacer.FinishedPlacingOrLoadingChip += OnChipPlaced;
			editor.ChipSelector.ChipSelected += (chip) => CancelWire();
			editor.PinInteractions.LeftMouseDown += OnMousePressPin;
			editor.PinInteractions.MouseEntered += OnMouseOverPin;
			editor.PinInteractions.MouseExitted += OnMouseExitPin;
			editor.PinInteractions.LeftMouseReleased += OnMouseReleasedOverPin;
			editor.WorkArea.WorkAreaMouseInteraction.LeftMouseDown += OnWorkAreaPressed;
		}


		void Update()
		{
			if (IsCreatingWire)
			{
				UpdateActiveWire();
			}

			Keyboard keyboard = Keyboard.current;
			if (keyboard.backspaceKey.wasPressedThisFrame)
			{
				OnBackspacePressed();
			}
		}

		void UpdateActiveWire()
		{
			if (MouseHelper.RightMousePressedThisFrame() || Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				CancelWire();
			}
			else
			{
				wireUnderConstruction.DrawToPoint(CalculateSnappedMousePosition());
			}
		}

		void OnWireConnected(Wire wire)
		{
			RegisterWire(wire);
			WireCreated?.Invoke(wire);
		}

		void RegisterWire(Wire wire)
		{
			allConnectedWires.Add(wire);
			wire.MouseInteraction.MouseEntered += OnMouseOverWire;
			wire.MouseInteraction.MouseExitted += OnMouseExitWire;
			wire.MouseInteraction.LeftMouseDown += OnWirePressed;
			wire.MouseInteraction.LeftMouseReleased += OnMouseUpOverWire;

			// Deleted event may not have been subscibed to already (for example if loading the wire, instead of creating it)
			wire.WireDeleted -= OnWireDeleted;
			wire.WireDeleted += OnWireDeleted;
		}

		Wire StartCreatingWire(Wire wireToStartFrom, Vector2 point)
		{
			wireStartWire = wireToStartFrom;
			return StartCreatingWire(point);
		}

		Wire StartCreatingWire(Pin pinToStartFrom)
		{
			wireStartPin = pinToStartFrom;
			return StartCreatingWire(pinToStartFrom.transform.position);
		}

		Wire StartCreatingWire(Vector2 point)
		{
			wireUnderConstruction = Instantiate(wirePrefab, wireHolder);
			wireUnderConstruction.AddAnchorPoint(point);
			wireUnderConstruction.WireDeleted += OnWireDeleted;
			return wireUnderConstruction;
		}


		void CancelWire()
		{
			if (IsCreatingWire)
			{
				wireUnderConstruction.DeleteWire();
				StopCreatingWire();
			}
		}

		void StopCreatingWire()
		{
			wireStartPin = null;
			wireStartWire = null;
			wireUnderConstruction = null;
		}


		void OnWireDeleted(Wire wire)
		{
			if (allConnectedWires.Contains(wire))
			{
				allConnectedWires.Remove(wire);
			}

			// Don't notify for wires that haven't actually been connected yet
			if (wire.IsConnected)
			{
				WireDeleted?.Invoke(wire);
			}
		}


		void OnMousePressPin(Pin pin)
		{
			if (chipEditor.CanEdit && !pin.IsBusPin)
			{
				if (IsCreatingWire)
				{
					TryMakeConnection(pin);
				}
				else
				{
					StartCreatingWire(pin);
				}
			}
		}

		void OnMouseReleasedOverPin(Pin pin)
		{
			if (IsCreatingWire)
			{
				TryMakeConnection(pin);
			}
		}

		void OnMouseOverPin(Pin pin)
		{
			if (IsCreatingWire)
			{
				var state = IsCreatingValidConnection(pin) ? Pin.HighlightState.Highlighted : Pin.HighlightState.HighlightedInvalid;
				pin.SetHighlightState(state);
			}
			else if (!chipEditor.ChipMover.IsBusy() && !chipEditor.ChipPlacer.IsBusy())
			{
				pin.SetHighlightState(Pin.HighlightState.Highlighted);
			}
		}

		void OnMouseExitPin(Pin pin)
		{
			pin.SetHighlightState(Pin.HighlightState.None);
		}

		void OnWorkAreaPressed(WorkArea w)
		{
			if (IsCreatingWire)
			{
				wireUnderConstruction.AddAnchorPoint(CalculateSnappedMousePosition());
			}
		}


		void OnMouseOverWire(Wire wire)
		{
			if (!chipEditor.ChipMover.IsBusy() && !chipEditor.ChipPlacer.IsBusy())
			{
				if (TryGetValidConnection(wire).success || !IsCreatingWire)
				{
					wire.SetHighlightState(true);
				}
			}
			wireUnderMouse = wire;
		}

		void OnMouseExitWire(Wire wire)
		{
			wire.SetHighlightState(false);

			if (wireUnderMouse == wire)
			{
				wireUnderMouse = null;
			}
		}

		void OnWirePressed(Wire wire)
		{
			if (chipEditor.CanEdit)
			{
				if (IsCreatingWire)
				{
					if (!TryMakeConnection(wire))
					{
						wireUnderConstruction.AddAnchorPoint(CalculateSnappedMousePosition());
					}
				}
				else
				{
					StartCreatingWire(wire, wire.ClosestPoint(MouseHelper.GetMouseWorldPosition()));
				}
			}
		}

		void OnMouseUpOverWire(Wire wire)
		{
			TryMakeConnection(wire);
		}

		void OnBackspacePressed()
		{
			if (chipEditor.CanEdit)
			{
				if (IsCreatingWire)
				{
					if (wireUnderConstruction.AnchorPoints.Count > 1)
					{
						wireUnderConstruction.RemoveLastAnchorPoint();
					}
					else
					{
						wireUnderConstruction.DeleteWire();
						StopCreatingWire();
					}
				}
				else if (wireUnderMouse is not null)
				{
					wireUnderMouse.DeleteWire();
					wireUnderMouse = null;
				}
			}
		}

		// Try connect the current wire to another wire
		bool TryMakeConnection(Wire connectingWire)
		{
			(bool success, Pin pinA, Pin pinB) = TryGetValidConnection(connectingWire);
			if (success)
			{
				if (connectingWire.IsBusWire)
				{
					wireUnderConstruction.AddAnchorPoint(connectingWire.ClosestPoint(wireUnderConstruction.CurrentDrawToPoint));
				}
				else
				{
					JoinToWire(MouseHelper.GetMouseWorldPosition(), connectingWire, pinB);
				}

				if (creatingWireFromWire && !wireStartWire.IsBusWire)
				{
					JoinFromWire(wireStartWire, pinA);
				}
				MakeConnection(pinA, pinB);
			}
			return success;

		}

		// Try connect the current wire to a pin
		void TryMakeConnection(Pin endPin)
		{
			(bool success, Pin pinA, Pin pinB) = TryGetValidConnection(endPin);
			if (success)
			{
				if (creatingWireFromWire && !wireStartWire.IsBusWire)
				{
					JoinFromWire(wireStartWire, pinA);
				}
				MakeConnection(pinA, pinB);
			}
		}

		// Try connect wire between the two given pins
		void TryMakeConnection(Pin startPin, Pin endPin)
		{
			if (IsValidConnection(startPin, endPin))
			{
				MakeConnection(startPin, endPin);
			}
		}

		void MakeConnection(Pin startPin, Pin endPin)
		{
			if (!endPin.IsBusPin)
			{
				wireUnderConstruction.AddAnchorPoint(endPin.transform.position);
			}
			wireUnderConstruction.ConnectWireToPins(startPin, endPin);

			Wire connectedWire = wireUnderConstruction;
			StopCreatingWire();
			OnWireConnected(connectedWire);
		}


		(bool success, Pin pinA, Pin pinB) TryGetValidConnection(Wire targetWire)
		{
			if (creatingWireFromPin)
			{
				if (targetWire.SourcePin == wireStartPin || targetWire.TargetPin == wireStartPin)
				{
					return (false, null, null);
				}
				if (IsValidConnection(wireStartPin, targetWire.SourcePin))
				{
					return (true, wireStartPin, targetWire.SourcePin);
				}
				if (IsValidConnection(wireStartPin, targetWire.TargetPin))
				{
					return (true, wireStartPin, targetWire.TargetPin);
				}
			}
			else if (creatingWireFromWire && targetWire != wireStartWire)
			{
				if (IsValidConnection(wireStartWire.SourcePin, targetWire.TargetPin))
				{
					return (true, wireStartWire.SourcePin, targetWire.TargetPin);
				}
			}
			return (false, null, null);
		}

		(bool success, Pin pinA, Pin pinB) TryGetValidConnection(Pin targetPin)
		{
			if (creatingWireFromPin && IsValidConnection(wireStartPin, targetPin))
			{
				return (true, wireStartPin, targetPin);
			}
			else if (creatingWireFromWire)
			{
				if (IsValidConnection(wireStartWire.SourcePin, targetPin))
				{
					return (true, wireStartWire.SourcePin, targetPin);
				}
				if (IsValidConnection(wireStartWire.TargetPin, targetPin))
				{
					return (true, wireStartWire.TargetPin, targetPin);
				}
			}
			return (false, null, null);
		}

		// Copies the anchor points from the given wire into the wire currently being constructed.
		// Starts from the start point, and copies up to the source or target pin (as specified).
		void JoinToWire(Vector2 startPoint, Wire targetWire, Pin targetWirePin)
		{
			bool copyToSource = targetWire.SourcePin == targetWirePin;
			List<Vector2> copyAnchorPoints = new List<Vector2>(targetWire.AnchorPoints);
			Vector2 closestPointOnWire = SebUtils.Maths.ClosestPointOnPath(startPoint, copyAnchorPoints, out int closestSegmentIndex);
			wireUnderConstruction.AddAnchorPoint(closestPointOnWire);

			int anchorIndex = (copyToSource) ? closestSegmentIndex : closestSegmentIndex + 1;
			do
			{
				wireUnderConstruction.AddAnchorPoint(copyAnchorPoints[anchorIndex]);
				anchorIndex += copyToSource ? -1 : 1;
			}
			while (anchorIndex >= 0 && anchorIndex <= copyAnchorPoints.Count - 1);

			// Insert the join point into the wire we're connecting to, to make the connection look more convincing
			//copyAnchorPoints.Insert(closestSegmentIndex + 1, closestPointOnWire);
			//targetWire.SetAnchorPoints(copyAnchorPoints, true);
		}

		// Copies the anchor points from the given wire into the wire currently being constructed.
		// Starts from the start point, and copies up to the source or target pin (as specified).
		void JoinFromWire(Wire targetWire, Pin targetWirePin)
		{
			bool copyToSource = targetWire.SourcePin == targetWirePin;
			List<Vector2> targetWirePoints = new List<Vector2>(targetWire.AnchorPoints);
			List<Vector2> newWirePoints = new List<Vector2>(wireUnderConstruction.AnchorPoints);

			Vector2 joinPoint = newWirePoints[0];
			joinPoint = SebUtils.Maths.ClosestPointOnPath(joinPoint, targetWirePoints, out int closestSegmentIndex);
			newWirePoints[0] = joinPoint;

			int anchorIndex = (copyToSource) ? closestSegmentIndex : closestSegmentIndex + 1;
			do
			{
				newWirePoints.Insert(0, targetWirePoints[anchorIndex]);
				anchorIndex += copyToSource ? -1 : 1;
			}
			while (anchorIndex >= 0 && anchorIndex <= targetWirePoints.Count - 1);

			// Insert the join point into the wire we're connecting to, to make the connection look more convincing
			//targetWirePoints.Insert(closestSegmentIndex + 1, joinPoint);
			//targetWire.SetAnchorPoints(targetWirePoints, true);
			wireUnderConstruction.SetAnchorPoints(newWirePoints, true);
		}

		void OnChipPlaced(ChipBase chip, bool wasLoaded)
		{
			if (chip is BusDisplay bus)
			{
				OnWireConnected(bus.Wire);
				//RegisterWire(bus.Wire);
			}
		}

		bool IsCreatingValidConnection(Pin endPin)
		{
			if (creatingWireFromPin)
			{
				return IsValidConnection(wireStartPin, endPin);
			}
			else if (creatingWireFromWire)
			{
				return IsValidConnection(wireStartWire.SourcePin, endPin) || IsValidConnection(wireStartWire.TargetPin, endPin);
			}
			return false;
		}

		bool IsValidConnection(Pin pinA, Pin pinB)
		{
			return validConnectionsLookup.Contains((pinA.GetPinType(), pinB.GetPinType()));
		}

		// Load a saved connection
		public void Load(ConnectionDescription connection)
		{
			Pin pinA = chipEditor.GetPin(connection.Source);
			Pin pinB = chipEditor.GetPin(connection.Target);

			Wire wire = Instantiate(wirePrefab, wireHolder);

			Vector2[] points = connection.WirePoints.Select(w => new Vector2(w.X, w.Y)).ToArray();
			// Update start and end points in case positions of subchip pins have been edited since this chip was last saved
			if (!pinA.IsBusPin)
			{
				points[0] = pinA.transform.position;
			}
			if (!pinB.IsBusPin)
			{
				points[^1] = pinB.transform.position;
			}
			wire.SetAnchorPoints(points, true);

			wire.ConnectWireToPins(pinA, pinB);
			OnWireConnected(wire);

			wire.SetColourTheme(chipEditor.ColourThemes.GetTheme(connection.ColourThemeName));
		}


		Vector2 CalculateSnappedMousePosition()
		{
			bool snap = Keyboard.current.leftShiftKey.isPressed;
			return MouseHelper.CalculateAxisSnappedMousePosition(wireUnderConstruction.AnchorPoints[^1], snap);
		}

		void InitValidConnectionLookup()
		{
			validConnectionsLookup = new HashSet<(PinType, PinType)>();
			AddValidConnection(PinType.ChipInputPin, PinType.ChipOutputPin);
			AddValidConnection(PinType.ChipInputPin, PinType.SubChipInputPin);
			AddValidConnection(PinType.SubChipInputPin, PinType.SubChipOutputPin);
			AddValidConnection(PinType.SubChipOutputPin, PinType.ChipOutputPin);

			void AddValidConnection(PinType a, PinType b)
			{
				validConnectionsLookup.Add((a, b));
				validConnectionsLookup.Add((b, a));
			}
		}


	}
}