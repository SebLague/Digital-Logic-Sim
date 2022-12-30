using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using DLS.Simulation.ChipImplementation;

namespace DLS.Simulation
{
	public static class BuiltinSimChipCreator
	{
		public static BuiltinSimChip CreateFromName(string name, SimPin[] inputPins, SimPin[] outputPins)
		{
			switch (name)
			{
				case BuiltinChipNames.AndChip: return new BuiltinAND(inputPins, outputPins);
				case BuiltinChipNames.NotChip: return new BuiltinNOT(inputPins, outputPins);
				case BuiltinChipNames.TriStateBufferName: return new BuiltinTriStateBuffer(inputPins, outputPins);
				case BuiltinChipNames.SevenSegmentDisplayName: return new BuiltingSevenSegmentDisplay(inputPins, outputPins);
				case BuiltinChipNames.BusName: return new BuiltinBus(inputPins, outputPins);
				case BuiltinChipNames.ClockName: return new BuiltinClock(inputPins, outputPins);
				default:
					Debug.LogError("Invalid builtin chip: " + name);
					return default;
			}
		}


	}
}
