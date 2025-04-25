namespace DLS.Simulation
{
	public class PinState
	{
		// Each bit has three possible states (tri-state logic):
		public const ushort LogicLow = 0;
		public const ushort LogicHigh = 1;
		public const ushort LogicDisconnected = 2;
		

		// LOW/HIGH state of each bit
		ushort bitStates;

		// If flag is set, it means the corresponding bit state is DISCONNECTED
		// (note: corresponding bit state is expected to be set to LOW in that case)
		ushort tristateFlags;
		

		public uint GetRawBits() => bitStates;
		public uint GetTristateFlags() => tristateFlags;
		public void SetTristateFlags(ushort v) => tristateFlags = v;
		public void SetRawBits(ushort v) => bitStates = v;

		public void SetAllBits_NoneDisconnected(ushort newBitStates)
		{
			bitStates = newBitStates;
			tristateFlags = 0;
		}

		public bool FirstBitHigh() => (bitStates & 1) == LogicHigh;

		public void SetBit(ushort bitIndex, ushort newState)
		{
			// Clear current state
			ushort mask = (ushort)~(1u << bitIndex);
			bitStates &= mask;
			tristateFlags &= mask;

			// Set new state
			bitStates |= (ushort)((newState & 1) << bitIndex);
			tristateFlags |= (ushort)((newState >> 1) << bitIndex);
		}

		public ushort GetBit(int bitIndex)
		{
			ushort state = (ushort)((bitStates >> bitIndex) & 1);
			ushort tri = (ushort)((tristateFlags >> bitIndex) & 1);
			return (ushort)(state | (tri << 1)); // Combine to form tri-stated value: 0 = LOW, 1 = HIGH, 2 = DISCONNECTED
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
				const ushort mask = 0b1111;
				bitStates = (ushort)(source8bit.bitStates & mask);
				tristateFlags = (ushort)(source8bit.tristateFlags & mask);
			}
			else
			{
				const uint mask = 0b11110000;
				bitStates = (ushort)((source8bit.bitStates & mask) >> 4);
				tristateFlags = (ushort)((source8bit.tristateFlags & mask) >> 4);
			}
		}

		public void Set8BitFrom4BitSources(PinState a, PinState b)
		{
			bitStates = (ushort)(a.bitStates | (b.bitStates << 4));
			tristateFlags = (ushort)(a.tristateFlags | (b.tristateFlags << 4));
		}

		public void Toggle(int bitIndex)
		{
			bitStates ^= (ushort)(1u << bitIndex);

			// Clear tristate flag (can't be disconnected if toggling)
			tristateFlags &= (ushort)~(1u << bitIndex);
		}

		public void SetAllDisconnected()
		{
			bitStates = 0;
			tristateFlags = ushort.MaxValue;
		}
	}
}