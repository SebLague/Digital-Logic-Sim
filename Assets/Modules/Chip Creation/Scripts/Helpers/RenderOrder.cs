namespace DLS.ChipCreation
{
	// Defines the order various elements should be rendererd in (by specifying their position on the z axis)
	// Negative numbers are closer to the camera
	public static class RenderOrder
	{

		public const float Background = 0;
		public const float BackgroundOutline = Background + layerAbove;

		public const float WireLow = BackgroundOutline + layerAbove;
		public const float WireHigh = WireLow + layerAbove;
		public const float BusWireLow = WireHigh + layerAbove;
		public const float BusWireHigh = BusWireLow + layerAbove;
		public const float WireEdit = BusWireHigh + layerAbove;

		public const float PinNameDisplay = WireEdit + layerAbove;

		public const float Chip = PinNameDisplay + layerAbove;
		public const float ChipPin = Chip + layerAbove;

		public const float EditablePin = ChipPin + layerAbove;
		public const float EditablePinHigh = EditablePin + layerAbove;
		public const float EditablePinPreview = EditablePinHigh + layerAbove;

		public const float ChipMoving = EditablePinPreview + layerAbove;

		// Step size
		public const float layerAbove = -0.01f;
	}
}
