namespace DLS.Simulation
{
	public class PinState
	{
		// Each bit has three possible states (tri-state logic):
		public const ushort LogicLow = 0;
		public const ushort LogicHigh = 1;
		public const ushort LogicDisconnected = 2;


		uint state;

		public static ushort GetBitStates(uint state) => (ushort)state;
		public static ushort GetTristateFlags(uint state) => (ushort)(state >> 16);

		public static void Set(ref uint state, ushort bitStates, ushort tristateFlags)
		{
			state = (uint)(bitStates | (tristateFlags << 16));
		}

		public static void Set(ref uint state, uint other)
		{
			state = other;
		}

		public static void SetBitStates(ref uint state, ushort bitStates) => Set(ref state, bitStates, GetTristateFlags(state));
		public static void SetTristateFlags(ref uint state, ushort flags) => Set(ref state, GetBitStates(state), flags);


		public static void SetBit(ref uint state, ushort bitIndex, ushort newState)
		{
			// Clear current state
			ushort mask = (ushort)~(1u << bitIndex);
			ushort bitStates = GetBitStates(state);
			ushort tristate = GetTristateFlags(state);

			bitStates &= mask;
			tristate &= mask;

			// Set new state
			bitStates |= (ushort)((newState & 1) << bitIndex);
			tristate |= (ushort)((newState >> 1) << bitIndex);

			Set(ref state, bitStates, tristate);
		}

		public static ushort GetBit(uint state, int bitIndex)
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
		
		public static void Set8BitFrom4BitSources(ref uint state, uint a, uint b)
		{
			ushort bitStates = (ushort)(GetBitStates(a) | (GetBitStates(b) << 4));
			ushort tristateFlags = (ushort)(GetTristateFlags(a) | (GetTristateFlags(b) << 4));
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
			

		// ----------- Instance methods


		public uint GetRawBits() => GetBitStates(state);
		public uint GetTristateFlags() => GetTristateFlags(state);
		public void SetTristateFlags(ushort v) => SetTristateFlags(ref state, v);
		public void SetRawBits(ushort v) => SetBitStates(ref state, v);

		public void SetAllBits_NoneDisconnected(ushort newBitStates)
		{
			Set(ref state, newBitStates, 0);
		}

		public bool FirstBitHigh() => FirstBitHigh(state);

		public void SetBit(ushort bitIndex, ushort newState) => SetBit(ref state, bitIndex, newState);

		public ushort GetBit(int bitIndex) => GetBit(state, bitIndex);

		public void SetFromSource(PinState source) => Set(ref state, source.state);

		public void Set4BitFrom8BitSource(PinState source8bit, bool firstNibble) => Set4BitFrom8BitSource(ref state, source8bit.state, firstNibble);

		public void Set8BitFrom4BitSources(PinState a, PinState b) => Set8BitFrom4BitSources(ref state, a.state, b.state);

		public void Toggle(int bitIndex) => Toggle(ref state, bitIndex);

		public void SetAllDisconnected() => SetAllDisconnected(ref state);
		
	}
}