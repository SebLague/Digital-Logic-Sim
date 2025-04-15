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
		public void Set16BitFrom4BitSources(PinState a, PinState b, PinState c, PinState d)
		{
			bitStates = a.bitStates | (b.bitStates << 4) | (c.bitStates << 12) | (d.bitStates << 8);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 4) | (c.tristateFlags << 8) | (d.tristateFlags << 12);
		}
		public void Set16BitFrom8BitSources(PinState a, PinState b)
		{
			bitStates = a.bitStates | (b.bitStates << 8);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 8);
		}

		public void Set8BitFrom16BitSource(PinState source16bit, bool firstByte)
		{
			if (firstByte)
			{
				const uint mask = 0b111111111111;
				bitStates = source16bit.bitStates & mask;
				tristateFlags = source16bit.tristateFlags & mask;
			}
			else
			{
				const uint mask = 0b1111111100000000;
				bitStates = (source16bit.bitStates & mask) >> 8;
				tristateFlags = (source16bit.tristateFlags & mask) >> 8;
			}
		}
		public void Set4BitFrom16BitSource(PinState source16bit, byte whichNibble)
		{
			if (whichNibble == 3)
			{
				const uint mask = 0b1111;
				bitStates = source16bit.bitStates & mask;
				tristateFlags = source16bit.tristateFlags & mask;
			}
			else if (whichNibble == 2)
			{
				const uint mask = 0b11110000;
				bitStates = (source16bit.bitStates & mask) >> 4;
				tristateFlags = (source16bit.tristateFlags & mask) >> 4;
			}
			else if (whichNibble == 1)
			{
				const uint mask = 0b111100000000;
				bitStates = (source16bit.bitStates & mask) >> 8;
				tristateFlags = (source16bit.tristateFlags & mask) >> 8;
			}
			else if (whichNibble == 0)
			{
				const uint mask = 0b1111000000000000;
				bitStates = (source16bit.bitStates & mask) >> 12;
				tristateFlags = (source16bit.tristateFlags & mask) >> 12;
			}
			else
			{
				throw new System.ArgumentOutOfRangeException(nameof(whichNibble), "Nibble index must be between 0 and 3.");
			}
		}

		public void Set4BitFrom32BitSource(PinState source16bit, byte whichNibble)
		{
			if (whichNibble == 0)
			{
				const uint mask = 0b1111;
				bitStates = source16bit.bitStates & mask;
				tristateFlags = source16bit.tristateFlags & mask;
			}
			else if (whichNibble == 1)
			{
				const uint mask = 0b11110000;
				bitStates = (source16bit.bitStates & mask) >> 4;
				tristateFlags = (source16bit.tristateFlags & mask) >> 4;
			}
			else if (whichNibble == 2)
			{
				const uint mask = 0b111100000000;
				bitStates = (source16bit.bitStates & mask) >> 8;
				tristateFlags = (source16bit.tristateFlags & mask) >> 8;
			}
			else if (whichNibble == 3)
			{
				const uint mask = 0b1111000000000000;
				bitStates = (source16bit.bitStates & mask) >> 12;
				tristateFlags = (source16bit.tristateFlags & mask) >> 12;
			}
			else if (whichNibble == 4)
			{
				const uint mask = 0b11110000000000000000;
				bitStates = (source16bit.bitStates & mask) >> 16;
				tristateFlags = (source16bit.tristateFlags & mask) >> 16;
			}
			else if (whichNibble == 5)
			{
				const uint mask = 0b111100000000000000000000;
				bitStates = (source16bit.bitStates & mask) >> 20;
				tristateFlags = (source16bit.tristateFlags & mask) >> 20;
			}
			else if (whichNibble == 6)
			{
				const uint mask = 0b11110000000000000000000000;
				bitStates = (source16bit.bitStates & mask) >> 24;
				tristateFlags = (source16bit.tristateFlags & mask) >> 24;
			}
			else if (whichNibble == 7)
			{
				const uint mask = 0b1111000000000000000000000000;
				bitStates = (source16bit.bitStates & mask) >> 28;
				tristateFlags = (source16bit.tristateFlags & mask) >> 28;
			}
			else if (whichNibble == 8)
			{
				const uint mask = 0b11110000000000000000000000000000;
				bitStates = (source16bit.bitStates & mask) >> 32;
				tristateFlags = (source16bit.tristateFlags & mask) >> 32;
			}
			else
			{
				throw new System.ArgumentOutOfRangeException(nameof(whichNibble), "Nibble index must be between 0 and 3.");
			}
		}


		public void Set32BitFrom4BitSources(PinState a, PinState b, PinState c, PinState d, PinState e, PinState f, PinState g, PinState h)
		{
			bitStates = a.bitStates | (b.bitStates << 4) | (c.bitStates << 8) | (d.bitStates << 12) |
						(e.bitStates << 16) | (f.bitStates << 20) | (g.bitStates << 24) | (h.bitStates << 28);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 4) | (c.tristateFlags << 8) | (d.tristateFlags << 12) |
							(e.tristateFlags << 16) | (f.tristateFlags << 20) | (g.tristateFlags << 24) | (h.tristateFlags << 28);
		}
		public void Set32BitFrom8BitSources(PinState a, PinState b, PinState c, PinState d){
			bitStates = a.bitStates | (b.bitStates << 8) | (c.bitStates << 16) | (d.bitStates << 24);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 8) | (c.tristateFlags << 16) | (d.tristateFlags << 24);
		}
		public void Set32BitFrom16BitSources(PinState a, PinState b)
		{
			bitStates = a.bitStates | (b.bitStates << 16);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 16);
		}


		public void Set8BitFrom32BitSource(PinState source16bit, byte whichByte)
		{
			if (whichByte == 0)
			{
				const uint mask = 0b11111111;
				bitStates = source16bit.bitStates & mask;
				tristateFlags = source16bit.tristateFlags & mask;
			}
			else if (whichByte == 1)
			{
				const uint mask = 0b1111111100000000;
				bitStates = (source16bit.bitStates & mask) >> 8;
				tristateFlags = (source16bit.tristateFlags & mask) >> 8;
			}
			else if (whichByte == 2)
			{
				const uint mask = 0b11111111000000000000;
				bitStates = (source16bit.bitStates & mask) >> 16;
				tristateFlags = (source16bit.tristateFlags & mask) >> 16;
			}
			else if (whichByte == 3)
			{
				const uint mask = 0b111111110000000000000000;
				bitStates = (source16bit.bitStates & mask) >> 24;
				tristateFlags = (source16bit.tristateFlags & mask) >> 24;
			}
			else
			{
				throw new System.ArgumentOutOfRangeException(nameof(whichByte), "Byte index must be between 0 and 3.");
			}
		}
		public void Set16BitFrom32BitSource(PinState source32bit, bool firstByte)
		{
			if (!firstByte)
			{
				const uint mask = 0b1111111111111111;
				bitStates = source32bit.bitStates & mask;
				tristateFlags = source32bit.tristateFlags & mask;
			}
			else if (firstByte)
			{
				const uint mask = 0b11111111111111110000000000000000;
				bitStates = (source32bit.bitStates & mask) >> 16;
				tristateFlags = (source32bit.tristateFlags & mask) >> 16;
			}
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