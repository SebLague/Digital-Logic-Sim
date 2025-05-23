using System;
using System.Collections.Generic;
using DLS.Description;
using DLS.Graphics;
using UnityEngine;

namespace DLS.Game
{
	public class WireInstance : IInteractable
	{
		public readonly PinBitCount bitCount;

		// Wire points for multi-bit wires. Note: must be populated with points when drawing
		public readonly BitWire[] BitWires;

		// The first connected pin when player is creating the wire
		public readonly PinInstance FirstPin;

		public readonly int spawnOrder;

		// Points defining shape of wire (first and last points are sourcePin and targetPin)
		readonly List<Vector2> WirePoints = new();

		// If this wire connects to a wire, which connects to another wire, then its recursion depth is 2. (this is used for ordering when drawing)
		public int ConnectedWireRecursionDepth;
		public int descriptionCreator_wireIndex;
		public int drawOrder;

		// An offset to be applied to all wire points (this is used when a wire is being moved, but the move has not yet been confirmed)
		public Vector2 MoveOffset;

		// For wires that connect to/from another wire, this is the original connection point (plus move offsets)
		// This allows the wire to keep its connection as close to this original point as possible when things are moved around.
		public Vector2 originalWireConnectionPoint;

		public ConnectionInfo SourceConnectionInfo;
		public ConnectionInfo TargetConnectionInfo;

		// Create wire from saved info
		public WireInstance(ConnectionInfo sourceConnection, ConnectionInfo targetConnection, Vector2[] points, int spawnOrder)
		{
			bitCount = sourceConnection.pin.bitCount;
			SourceConnectionInfo = sourceConnection;
			TargetConnectionInfo = targetConnection;

			WirePoints = new List<Vector2>(points);
			originalWireConnectionPoint = sourceConnection.IsConnectedAtWire ? points[0] : points[^1];
			BitWires = new BitWire[(int)bitCount];
			ConnectedWireRecursionDepth = CalculateConnectedWireRecursionDepth(this);
			this.spawnOrder = spawnOrder;
			InitCommon();
		}

		// Create incomplete wire (used when starting to place the wire)
		public WireInstance(ConnectionInfo firstConnectionInfo, int spawnOrder)
		{
			PinInstance firstPin = firstConnectionInfo.pin;
			FirstPin = firstPin;

			if (firstPin.IsSourcePin) SourceConnectionInfo = firstConnectionInfo;
			else TargetConnectionInfo = firstConnectionInfo;

			bitCount = firstPin.bitCount;
			originalWireConnectionPoint = firstConnectionInfo.connectionPoint;
			WirePoints.Add(GetAttachmentPoint(firstConnectionInfo));
			WirePoints.Add(WirePoints[0]); // end point to be controlled by mouse during placement mode

			BitWires = new BitWire[(int)bitCount];
			this.spawnOrder = spawnOrder;
			InitCommon();
		}

		// The pin from which this wire receives its signal
		public PinInstance SourcePin => SourceConnectionInfo.pin;

		// The pin to which this wire carries its signal
		public PinInstance TargetPin => TargetConnectionInfo.pin;
		public PinInstance TargetPin_BusCorrected => GetBusCorrectedTargetPin(this);

		public int WirePointCount => WirePoints.Count;
		public bool IsFullyConnected => SourcePin != null && TargetPin != null;

		public bool IsBusWire => IsFullyConnected && SourcePin.IsBusPin && TargetPin.IsBusPin;
		public ConnectionInfo FirstConnectionInfo => SourcePin == null ? TargetConnectionInfo : SourceConnectionInfo;

		// If this wire was connect to/from another wire, then that wire can be accessed here (otherwise is null)
		public WireInstance ConnectedWire => SourceConnectionInfo.connectedWire ?? TargetConnectionInfo.connectedWire;

		// For regular wires, the "bus corrected target" is just the normal target pin, but for bus wires it is overriden to point to the input pin of the bus origin.
		// This is because the bus wire connects from the bus origin to the bus terminus, but this is for graphical purposes only...
		// When a wire is connected to the bus, we actually want to connect it to either the input or output of the bus origin (and ignore the terminus entirely).
		static PinInstance GetBusCorrectedTargetPin(WireInstance wire) => wire.IsBusWire ? ((SubChipInstance)wire.SourcePin.parent).InputPins[0] : wire.TargetPin;

		void InitCommon()
		{
			InitBitWires();
		}

		int CalculateConnectedWireRecursionDepth(WireInstance wire, int depth = 0)
		{
			if (wire.ConnectedWire == null) return depth;
			return CalculateConnectedWireRecursionDepth(wire.ConnectedWire, depth + 1);
		}

		void InitBitWires()
		{
			for (int i = 0; i < BitWires.Length; i++)
			{
				BitWires[i] = new BitWire();
			}
		}


		// Finish placing the wire
		public void FinishPlacingWire(ConnectionInfo connection)
		{
			PinInstance endPin = connection.pin;
			WirePoints[^1] = endPin.GetWorldPos();

			// If wire connection started out at another wire, it is not known (until now when we have the end pin) whether that
			// initial connection should be to the source or target pin of that wire (so correct if needed)
			ConnectionInfo correctedFirstConnection = FirstConnectionInfo;
			if (FirstConnectionInfo.pin.IsSourcePin == endPin.IsSourcePin) // same type, must fix
			{
				Debug.Assert(FirstConnectionInfo.IsConnectedAtWire, "Connection is source->source or target->target, but connection didn't start from wire?!");
				if (endPin.IsSourcePin) correctedFirstConnection.pin = GetBusCorrectedTargetPin(FirstConnectionInfo.connectedWire);
				else correctedFirstConnection.pin = FirstConnectionInfo.connectedWire.SourcePin;
			}

			if (endPin.IsSourcePin)
			{
				// If started placing wire from the target pin, flip the wire points around so always flow from source to target
				WirePoints.Reverse();
				TargetConnectionInfo = correctedFirstConnection;
				SourceConnectionInfo = connection;
			}
			else
			{
				SourceConnectionInfo = correctedFirstConnection;
				TargetConnectionInfo = connection;
			}

			originalWireConnectionPoint = SourceConnectionInfo.IsConnectedAtWire ? SourceConnectionInfo.connectionPoint : TargetConnectionInfo.connectionPoint;
			ConnectedWireRecursionDepth = CalculateConnectedWireRecursionDepth(this);
		}

		Vector2 GetAttachmentPoint(ConnectionInfo connection)
		{
			if (connection.IsConnectedAtWire)
			{
				Vector2 posA = connection.connectedWire.GetWirePoint(connection.wireConnectionSegmentIndex);
				Vector2 posB = connection.connectedWire.GetWirePoint(connection.wireConnectionSegmentIndex + 1);
				Vector2 connectPos = ClosestPointOnLineSegment(originalWireConnectionPoint + MoveOffset, posA, posB);
				return connectPos;
			}

			return connection.pin.GetWorldPos();
		}


		public Vector2 GetWirePoint(int i)
		{
			if (IsFullyConnected)
			{
				// Get start/end point specially, since these can change if another chip is moved for example
				if (i == 0) return GetAttachmentPoint(SourceConnectionInfo);
				if (i == WirePointCount - 1) return GetAttachmentPoint(TargetConnectionInfo);
			}
			else
			{
				// If not fully connected then end point is controlled by mouse, so we don't want to override
				ConnectionInfo firstConnectionInfo = SourcePin == null ? TargetConnectionInfo : SourceConnectionInfo;
				if (i == 0) return GetAttachmentPoint(firstConnectionInfo);
			}

			// Other points
			return WirePoints[i] + MoveOffset;
		}

		public void ApplyMoveOffset()
		{
			for (int i = 0; i < WirePoints.Count; i++)
			{
				WirePoints[i] += MoveOffset;
			}

			originalWireConnectionPoint += MoveOffset;
			MoveOffset = Vector2.zero;
		}

		public void SetWirePoint(Vector2 p, int i)
		{
			bool isConnectedToWireAtThisPoint = (i == 0 && SourceConnectionInfo.IsConnectedAtWire) || (i == WirePointCount - 1 && TargetConnectionInfo.IsConnectedAtWire);
			if (isConnectedToWireAtThisPoint)
			{
				(Vector2 bestPoint, int bestSegmentIndex) = WireLayoutHelper.GetClosestPointOnWire(ConnectedWire, p);

				ref ConnectionInfo connectionInfo = ref GetWireConnectionInfo();
				connectionInfo.wireConnectionSegmentIndex = bestSegmentIndex;
				connectionInfo.connectionPoint = bestPoint;
				originalWireConnectionPoint = bestPoint;
			}

			WirePoints[i] = p;
		}

		public void SetWirePointWithSnapping(Vector2 p, int i, Vector2 straightLineRefPoint)
		{
			if (Project.ActiveProject.ShouldSnapToGrid) p = GridHelper.SnapToGrid(p, true, true);
			if (Project.ActiveProject.ForceStraightWires) p = GridHelper.ForceStraightLine(straightLineRefPoint, p);
			
			SetWirePoint(p, i);
		}

		public void SetLastWirePoint(Vector2 p)
		{
			SetWirePointWithSnapping(p, WirePoints.Count - 1, GetWirePoint(WirePoints.Count - 2));
		}

		public void AddWirePoint(Vector2 p) => WirePoints.Add(p);

		public void DeleteWirePoint(int i)
		{
			WirePoints.RemoveAt(i);
		}

		public void InsertPoint(Vector2 p, int segmentIndex)
		{
			float insertionT = PointOnSegmentT(GetWirePoint(segmentIndex), GetWirePoint(segmentIndex + 1), p);
			Project.ActiveProject.ViewedChip.NotifyConnectedWiresPointsInserted(this, segmentIndex, insertionT, 1);
			WirePoints.Insert(segmentIndex + 1, p);
		}

		public bool RemoveLastPoint()
		{
			if (WirePoints.Count > 2)
			{
				WirePoints.RemoveAt(WirePoints.Count - 1);
				return true;
			}

			return false;
		}

		// Connection info for the end of this wire that connects to another wire
		public ref ConnectionInfo GetWireConnectionInfo()
		{
			Debug.Assert(ConnectedWire != null, "No connected wire?");
			return ref SourceConnectionInfo.IsConnectedAtWire ? ref SourceConnectionInfo : ref TargetConnectionInfo;
		}

		public void NotifyParentWirePointsInserted(int insertIndex, float insertPointT, int num)
		{
			ref ConnectionInfo connectionInfo = ref GetWireConnectionInfo();
			if (connectionInfo.wireConnectionSegmentIndex < insertIndex) return; // connected point is before insertion point, so is unaffected

			if (connectionInfo.wireConnectionSegmentIndex == insertIndex)
			{
				Vector2 connectionSegA = ConnectedWire.GetWirePoint(connectionInfo.wireConnectionSegmentIndex);
				Vector2 connectionSegB = ConnectedWire.GetWirePoint(connectionInfo.wireConnectionSegmentIndex + 1);
				Vector2 p = GetAttachmentPoint(connectionInfo);
				float selfT = PointOnSegmentT(connectionSegA, connectionSegB, p);
				if (selfT < insertPointT) return; // connected point is on same segment as inserted point, but is before it, and so is unaffected
			}

			connectionInfo.wireConnectionSegmentIndex += num;
		}

		static float PointOnSegmentT(Vector2 a, Vector2 b, Vector2 p)
		{
			return (p - a).magnitude / (b - a).magnitude;
		}

		public void NotifyParentWirePointWillBeDeleted(int deletedPointIndex)
		{
			ref ConnectionInfo connectionInfo = ref GetWireConnectionInfo();

			if (connectionInfo.wireConnectionSegmentIndex >= deletedPointIndex) connectionInfo.wireConnectionSegmentIndex -= 1;
		}

		// If this wire connects to another wire that is about to be deleted, we want to remove that dependency by 
		// copying over the deleted wire's points and connection info to this wire so it can continue to exist without the deleted wire.
		public void RemoveConnectionDependency()
		{
			WireInstance dependency = ConnectedWire;
			bool connectToSource = SourceConnectionInfo.IsConnectedAtWire;

			if (connectToSource)
			{
				List<Vector2> pointsToCopy = new();
				for (int i = 0; i <= SourceConnectionInfo.wireConnectionSegmentIndex; i++)
				{
					Vector2 copyPoint = dependency.GetWirePoint(i);
					pointsToCopy.Add(copyPoint);
				}

				WirePoints[0] = GetWirePoint(0);
				WirePoints.InsertRange(0, pointsToCopy);

				// if some other wire connects to this wire, it needs to know how many points have been inserted here so that it can still connect in the correct place
				Project.ActiveProject.ViewedChip.NotifyConnectedWiresPointsInserted(this, -1, 0, pointsToCopy.Count);
				originalWireConnectionPoint = dependency.originalWireConnectionPoint;
				SourceConnectionInfo = dependency.SourceConnectionInfo;
			}
			else
			{
				// Connecting to target pin via another wire, it is only allowed if that other wire is a bus wire.
				// If the bus wire is being deleted, we don't need to bother removing the dependency because all connected wires will be deleted along with it.
				if (!ConnectedWire.IsBusWire) throw new Exception("Connected to target pin via non-bus wire??");
			}
		}

		public Color GetColour(int bitIndex)
		{
			Color col = IsFullyConnected ? SourcePin.GetStateCol(bitIndex, false, false) : DrawSettings.ActiveTheme.StateDisconnectedCol;

			if (bitCount != PinBitCount.Bit1 && bitIndex % 2 == 0)
			{
				Color alternatingWireHighlightDisconnected = Color.white * 0.075f;
				Color alternatingWireHighlightConnected = Color.white * 0.01f;
				col += IsFullyConnected ? alternatingWireHighlightConnected : alternatingWireHighlightDisconnected;
			}

			return col;
		}

		public static Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 a1, Vector2 a2)
		{
			Vector2 lineDelta = a2 - a1;
			Vector2 pointDelta = p - a1;
			float sqrLineLength = lineDelta.sqrMagnitude;

			if (sqrLineLength == 0) return a1;

			float t = Mathf.Clamp01(Vector3.Dot(pointDelta, lineDelta) / sqrLineLength);
			return a1 + lineDelta * t;
		}

		public struct ConnectionInfo
		{
			public PinInstance pin;

			// ---- Properties for a connection to another wire, rather than directly to a pin ----
			public WireInstance connectedWire;
			public int wireConnectionSegmentIndex; // Index of first point on segment where this connection is made
			public Vector2 connectionPoint;

			public bool IsConnectedAtWire => connectedWire != null;
		}


		public class BitWire
		{
			public Vector2[] Points = Array.Empty<Vector2>();
		}
	}
}