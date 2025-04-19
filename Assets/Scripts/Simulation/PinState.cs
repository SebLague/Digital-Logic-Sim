using System;
using UnityEngine;

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
		UInt64 bitStates;

		// If flag is set, it means the corresponding bit state is DISCONNECTED
		// (note: corresponding bit state is expected to be set to LOW in that case)
		UInt64 tristateFlags;

		public PinState(int numBits)
		{
			BitCount = numBits;
		}

		public UInt64 GetRawBits() => bitStates;
		public UInt64 GetTristateFlags() => tristateFlags;

		public void SetAllBits_NoneDisconnected(UInt64 newBitStates)
		{
			bitStates = newBitStates;
			tristateFlags = 0;
		}
		
		public void SetAllBits(UInt64 newBitStates)
		{
			bitStates = newBitStates;
		}
		public void SetAllTristateFlags(UInt64 _tristateFlags)
		{
			this.tristateFlags = _tristateFlags;
		}

		public bool FirstBitHigh() => (bitStates & 1) == LogicHigh;

		public void SetBit(int bitIndex, UInt64 newState)
		{
			UInt64 bit = (UInt64)1UL << bitIndex;
			UInt64 mask = ~bit;
			bitStates = (newState & 1) != 0 ? (bitStates | bit) : (bitStates & mask);
			tristateFlags = (newState & 2) != 0 ? (tristateFlags | bit) : (tristateFlags & mask);
		}

		public UInt64 GetBit(int bitIndex)
		{
			UInt64 state = (bitStates >> bitIndex) & 1;
			UInt64 tri = (tristateFlags >> (bitIndex - 1)) & 2;
			return state | tri; // Combine to form tri-stated value: 0 = LOW, 1 = HIGH, 2 = DISCONNECTED
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
		
		public void Set8BitFrom16BitSource(PinState source16bit, bool firstNibble)
		{
			if (firstNibble)
			{
				const uint mask = 0b11111111;
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

		public void Set8BitFrom4BitSources(PinState a, PinState b)
		{
			bitStates = a.bitStates | (b.bitStates << 4);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 4);
		}
		
		public void Set16BitFrom8BitSources(PinState a, PinState b)
		{
			bitStates = a.bitStates | (b.bitStates << 8);
			tristateFlags = a.tristateFlags | (b.tristateFlags << 8);
		}
		
		public void Set16BitFrom8BitValues(uint a, uint b)
		{
			bitStates = a | (b << 8);
			tristateFlags = a | (b << 8);
		}

		public void Toggle(int bitIndex)
		{
			bitStates ^= 1ul << bitIndex;

			// Clear tristate flag (can't be disconnected if toggling)
			tristateFlags &= ~(1ul << bitIndex);
		}

		public void SetAllDisconnected()
		{
			bitStates = 0;
			tristateFlags = (1ul << BitCount) - 1;
		}
	}
}