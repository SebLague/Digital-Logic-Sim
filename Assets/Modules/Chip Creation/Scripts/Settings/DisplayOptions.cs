namespace DLS.ChipCreation
{
	[System.Serializable]
	public struct DisplayOptions
	{
		public enum PinNameDisplayMode { Always, Hover, Toggle, Never }
		public enum ToggleState { Off, On }

		public PinNameDisplayMode MainChipPinNameDisplayMode;
		public PinNameDisplayMode SubChipPinNameDisplayMode;
		public ToggleState ShowCursorGuide;
	}
}