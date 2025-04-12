using UnityEngine;

namespace DLS.Description
{
	public enum WireConnectionType
	{
		ToPins, // both ends of wire connect directly to a pin
		ToWireSource, // source end of wire connects to another wire (target connects to pin)
		ToWireTarget // target end of wire connects to another wire (source connects to pin)
	}

	public struct WireDescription
	{
		public PinAddress SourcePinAddress;
		public PinAddress TargetPinAddress;
		public WireConnectionType ConnectionType;
		public int ConnectedWireIndex;
		public int ConnectedWireSegmentIndex;
		public Vector2[] Points;
	}
}