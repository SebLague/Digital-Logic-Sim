namespace DLS.Simulation
{
	public class PinState
	{
		// Each bit has three possible states (tri-state logic):
		public const uint LogicLow = 0;
		public const uint LogicHigh = 1;
		public const uint LogicDisconnected = 2;

		public readonly int BitCount;

		// LOW/HIGH state of each bit
		uint bitStates;

		// If flag is set, it means the corresponding bit state is DISCONNECTED
		// (note: corresponding bit state is expected to be set to LOW in that case)
		uint tristateFlags;

		public PinState(int numBits)
		{
			BitCount = numBits;
		}

		public uint GetRawBits() => bitStates;

		public void SetAllBits_NoneDisconnected(uint newBitStates)
		{
			bitStates = newBitStates;
			tristateFlags = 0;
		}

		public bool FirstBitHigh() => (bitStates & 1) == LogicHigh;

		public void SetBit(int bitIndex, uint newState)
		{
			// Clear current state
			uint mask = ~(1u << bitIndex);
			bitStates &= mask;
			tristateFlags &= mask;

			// Set new state
			bitStates |= (newState & 1) << bitIndex;
			tristateFlags |= (newState >> 1) << bitIndex;
		}

		public uint GetBit(int bitIndex)
		{
			uint state = (bitStates >> bitIndex) & 1;
			uint tri = (tristateFlags >> bitIndex) & 1;
			return state | (tri << 1); // Combine to form tri-stated value: 0 = LOW, 1 = HIGH, 2 = DISCONNECTED
		}

		public void SetFromSource(PinState source)
		{
			bitStates = source.bitStates;
			tristateFlags = source.tristateFlags;
		}

		public void Set4BitFrom8BitSource(PinState source8bit, bool firstNibble)
		{
			if (firstNibble)
			{
				const uint mask = 0b1111;
				bitStates = source8bit.bitStates & mask;
				tristateFlags = source8bit.tristateFlags & mask;
			}
			else
			{
				const uint mask = 0b11110000;
				bitStates = (source8bit.bitStates & mask) >> 4;
				tristateFlags = (source8bit.tristateFlags & mask) >> 4;
			}
		}

		public void Set8BitFrom4BitSources(PinState a, PinState b)
		{
			bitStates = a.bitStates | (b.bitStates << 4);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 4);
		}

		public void Toggle(int bitIndex)
		{
			bitStates ^= 1u << bitIndex;

			// Clear tristate flag (can't be disconnected if toggling)
			tristateFlags &= ~(1u << bitIndex);
		}

		public void SetAllDisconnected()
		{
			bitStates = 0;
			tristateFlags = (1u << BitCount) - 1;
		}
	}
}