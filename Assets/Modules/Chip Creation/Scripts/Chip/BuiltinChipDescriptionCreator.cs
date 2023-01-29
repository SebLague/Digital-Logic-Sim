using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	public class BuiltinChipDescriptionCreator
	{

		public static ChipDescription[] CreateBuiltinChipDescriptions()
		{

			return new ChipDescription[]
			{
				CreateBuiltinAND(),
				CreateBuiltinNOT(),
				CreateClockDescription(),
				CreateBuiltinTriStateBuffer(),
				CreateBuiltinSevenSegmentDisplay(),
				CreateBusDescription()
				
			};
		}

		static ChipDescription CreateBuiltinAND()
		{
			string name = BuiltinChipNames.AndChip;
			string[] inputNames = new string[] { "In A", "In B" };
			string[] outputNames = new string[] { "Out" };
			Color col = new Color(0.15f, 0.48f, 0.70f);
			return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
		}

		static ChipDescription CreateBuiltinNOT()
		{
			string name = BuiltinChipNames.NotChip;
			string[] inputNames = new string[] { "In A" };
			string[] outputNames = new string[] { "Out" };
			Color col = new Color(0.55f, 0.12f, 0.1f);
			return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
		}

		static ChipDescription CreateBuiltinTriStateBuffer()
		{
			string name = BuiltinChipNames.TriStateBufferName;
			string[] inputNames = new string[] { "Enable", "Data" };
			string[] outputNames = new string[] { "Out" };
			Color col = new Color(0.15f, 0.15f, 0.15f);
			return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
		}

		static ChipDescription CreateBuiltinSevenSegmentDisplay()
		{
			string name = BuiltinChipNames.SevenSegmentDisplayName;
			string[] inputNames = new string[] { "S", "A", "B", "C", "D", "E", "F", "G" };
			string[] outputNames = new string[] { };
			Color col = new Color(0.04f, 0.04f, 0.04f);
			return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
		}

		static ChipDescription CreateBusDescription()
		{
			string name = BuiltinChipNames.BusName;
			string[] inputNames = new string[] { "A" };
			string[] outputNames = new string[] { "B" };
			Color col = new Color(0, 0, 0);
			return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
		}

		static ChipDescription CreateClockDescription()
		{
			string name = BuiltinChipNames.ClockName;
			string[] inputNames = new string[] { "Freq 1", "Freq 0" };
			string[] outputNames = new string[] { "Out" };
			Color col = new Color(0, 0, 0);
			return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
		}
	}
}
