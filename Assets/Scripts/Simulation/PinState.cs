namespace DLS.Simulation
{
	// Helper class for dealing with pin state.
	// Pin state is stored as a uint32, with format:
	// Tristate flags (most significant 16 bits) | Bit states (least significant 16 bits)
	public static class PinState
	{
		// Each bit has three possible states (tri-state logic):
		public const ushort LogicLow = 0;
		public const ushort LogicHigh = 1;
		public const ushort LogicDisconnected = 2;

		// Mask for single bit value (bit state, and tristate flag)
		public const uint SingleBitMask = 1 | (1 << 16);
		
		public static ushort GetBitStates(uint state) => (ushort)state;
		public static ushort GetTristateFlags(uint state) => (ushort)(state >> 16);

		public static void Set(ref uint state, ushort bitStates, ushort tristateFlags)
		{
			state = (uint)(bitStates | (tristateFlags << 16));
		}

		public static void Set(ref uint state, uint other) => state = other;

		public static ushort GetBitTristatedValue(uint state, int bitIndex)
		{
			ushort bitState = (ushort)((GetBitStates(state) >> bitIndex) & 1);
			ushort tri = (ushort)((GetTristateFlags(state) >> bitIndex) & 1);
			return (ushort)(bitState | (tri << 1)); // Combine to form tri-stated value: 0 = LOW, 1 = HIGH, 2 = DISCONNECTED
		}

		public static bool FirstBitHigh(uint state) => (state & 1) == LogicHigh;

		public static void Set4BitFrom8BitSource(ref uint state, uint source8bit, bool firstNibble)
		{
			ushort sourceBitStates = GetBitStates(source8bit);
			ushort sourceTristateFlags = GetTristateFlags(source8bit);

			if (firstNibble)
			{
				const ushort mask = 0b1111;
				Set(ref state, (ushort)(sourceBitStates & mask), (ushort)(sourceTristateFlags & mask));
			}
			else
			{
				const uint mask = 0b11110000;
				Set(ref state, (ushort)((sourceBitStates & mask) >> 4), (ushort)((sourceTristateFlags & mask) >> 4));
			}
		}

		public static void Set8BitFrom16BitSource(ref uint state, uint source16bit, bool firstNibble)
		{
			ushort sourceBitStates = GetBitStates(source16bit);
			ushort sourceTristateFlags = GetTristateFlags(source16bit);

			if (firstNibble)
			{
				const ushort mask = 0b11111111;
				Set(ref state, (ushort)(sourceBitStates & mask), (ushort)(sourceTristateFlags & mask));
			}
			else
			{
				const uint mask = 0b1111111100000000;
				Set(ref state, (ushort)((sourceBitStates & mask) >> 8), (ushort)((sourceTristateFlags & mask) >> 8));
			}
		}
		
		public static void Set4BitFrom16BitSource(ref uint state, uint source16Bit, int section)
		{
			ushort sourceBitStates = GetBitStates(source16Bit);
			ushort sourceTristateFlags = GetTristateFlags(source16Bit);

			if (section == 0)
			{
				const ushort mask = 0b1111;
				Set(ref state, (ushort)(sourceBitStates & mask), (ushort)(sourceTristateFlags & mask));
			}
			else if (section == 1)
			{
				const uint mask = 0b11110000;
				Set(ref state, (ushort)((sourceBitStates & mask) >> 4), (ushort)((sourceTristateFlags & mask) >> 4));
			}
			else if (section == 2)
			{
				const uint mask = 0b111100000000;
				Set(ref state, (ushort)((sourceBitStates & mask) >> 8), (ushort)((sourceTristateFlags & mask) >> 8));
			}
			else if (section == 3)
			{
				const uint mask = 0b1111000000000000;
				Set(ref state, (ushort)((sourceBitStates & mask) >> 12), (ushort)((sourceTristateFlags & mask) >> 12));
			}
		}

		public static void Set8BitFrom4BitSources(ref uint state, uint a, uint b)
		{
			ushort bitStates = (ushort)(GetBitStates(a) | (GetBitStates(b) << 4));
			ushort tristateFlags = (ushort)((GetTristateFlags(a) & 0b1111) | ((GetTristateFlags(b) & 0b1111) << 4));
			Set(ref state, bitStates, tristateFlags);
		}

		public static void Set16BitFrom4BitSources(ref uint state, uint a, uint b, uint c, uint d)
		{
			ushort bitStates1 = (ushort)(GetBitStates(a) | (GetBitStates(b) << 4));
			ushort tristateFlags1 = (ushort)((GetTristateFlags(a) & 0b1111) | ((GetTristateFlags(b) & 0b1111) << 4));
			ushort bitStates2 = (ushort)(GetBitStates(c) | (GetBitStates(d) << 4));
			ushort tristateFlags2 = (ushort)((GetTristateFlags(c) & 0b1111) | ((GetTristateFlags(d) & 0b1111) << 4));
			ushort bitStates = (ushort)(GetBitStates(bitStates1) | (GetBitStates(bitStates2) << 8));
			ushort tristateFlags = (ushort)((GetTristateFlags(bitStates1) & 0b11111111) | ((GetTristateFlags(bitStates2) & 0b11111111) << 8));
			Set(ref state, bitStates, tristateFlags);
		}

		public static void Set16BitFrom8BitSources(ref uint state, uint a, uint b)
		{
			ushort bitStates = (ushort)(GetBitStates(a) | (GetBitStates(b) << 8));
			ushort tristateFlags = (ushort)((GetTristateFlags(a) & 0b11111111) | ((GetTristateFlags(b) & 0b11111111) << 8));
			Set(ref state, bitStates, tristateFlags);
		}


		public static void Toggle(ref uint state, int bitIndex)
		{
			ushort bitStates = GetBitStates(state);
			bitStates ^= (ushort)(1u << bitIndex);

			// Clear tristate flags (can't be disconnected if toggling as only input dev pins are allowed)
			Set(ref state, bitStates, 0);
		}

		public static void SetAllDisconnected(ref uint state) => Set(ref state, 0, ushort.MaxValue);
	}
}