using System;
using UnityEngine;
using System.ComponentModel;


namespace DLS.Description
{
	public class ChipDescription
	{
		// ---- Name Comparison ----
		public const StringComparison NameComparison = StringComparison.OrdinalIgnoreCase;
		public static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

		// ---- Data ----
		public string Name;
		public NameDisplayLocation NameLocation;
		[DefaultValue(NameAlignment.Centre)]
		public NameAlignment NameAlignment;
		public ChipType ChipType;
		public Vector2 Size;
		public Color Colour;
		public PinDescription[] InputPins;
		public PinDescription[] OutputPins;
		public SubChipDescription[] SubChips;
		public WireDescription[] Wires;
		public DisplayDescription[] Displays;

		// ---- Convenience Functions ----
		public bool HasDisplay() => Displays != null && Displays.Length > 0;
		public bool NameMatch(string otherName) => NameMatch(Name, otherName);
		public static bool NameMatch(string a, string b) => string.Equals(a, b, NameComparison);
	}

	public enum NameDisplayLocation
	{
		Centre,
		Top,
		Hidden
	}

	public enum NameAlignment
	{
		Centre,
		Right,
		Left
	}
}