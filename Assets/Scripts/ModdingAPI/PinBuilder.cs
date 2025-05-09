using UnityEngine;

namespace DLS.ModdingAPI
{
    public struct PinDescription
	{
		public string Name;
		public int ID;
		public Vector2 Position;
		public PinBitCount BitCount;
		public PinColour Colour;
		public PinValueDisplayMode ValueDisplayMode;

		public PinDescription(string name, int id, Vector2 position, PinBitCount bitCount, PinColour colour, PinValueDisplayMode valueDisplayMode)
		{
			Name = name;
			ID = id;
			Position = position;
			BitCount = bitCount;
			Colour = colour;
			ValueDisplayMode = valueDisplayMode;
		}

		public PinDescription(string name, int id)
		{
			Name = name;
			ID = id;
			Position = Vector2.zero;
			BitCount = PinBitCount.Bit1;
			Colour = PinColour.Red;
			ValueDisplayMode = PinValueDisplayMode.Off;
		}
	}

	public enum PinBitCount
	{
		Bit1 = 1,
		Bit4 = 4,
		Bit8 = 8
	}

	public enum PinColour
	{
		Red,
		Orange,
		Yellow,
		Green,
		Blue,
		Violet,
		Pink,
		White
	}

	public enum PinValueDisplayMode
	{
		Off,
		UnsignedDecimal,
		SignedDecimal,
		HEX
	}
}