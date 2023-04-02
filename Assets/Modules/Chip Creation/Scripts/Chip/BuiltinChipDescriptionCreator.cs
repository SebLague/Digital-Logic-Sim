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
				CreateBuiltinOR(),
				CreateBuiltinXOR(),
				CreateBuiltinNAND(),
				CreateBuiltinNOR(),
				CreateBuiltinXNOR(),
				CreateClockDescription(),
                CreateBuiltinTriStateBuffer(),
				CreateBuiltinSevenSegmentDisplay(),
				CreateBusDescription(),
				CreateTickDelayDescription(),
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

        static ChipDescription CreateBuiltinOR()
        {
            string name = BuiltinChipNames.OrChip;
            string[] inputNames = new string[] { "In A", "In B" };
            string[] outputNames = new string[] { "Out" };
            Color col = new Color(0.14f, 0.70f, 0.32f);
            return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
        }

        static ChipDescription CreateBuiltinXOR()
        {
            string name = BuiltinChipNames.XorChip;
            string[] inputNames = new string[] { "In A", "In B" };
            string[] outputNames = new string[] { "Out" };
            Color col = new Color(0.70f, 0.20f, 0.15f);
            return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
        }

        static ChipDescription CreateBuiltinNAND()
        {
            string name = BuiltinChipNames.NandChip;
            string[] inputNames = new string[] { "In A", "In B" };
            string[] outputNames = new string[] { "Out" };
            Color col = new Color(0.85f, 0.52f, 0.30f);
            return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
        }

        static ChipDescription CreateBuiltinNOR()
        {
            string name = BuiltinChipNames.NorChip;
            string[] inputNames = new string[] { "In A", "In B" };
            string[] outputNames = new string[] { "Out" };
            Color col = new Color(0.85f, 0.30f, 0.63f);
            return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
        }

        static ChipDescription CreateBuiltinXNOR()
        {
            string name = BuiltinChipNames.XnorChip;
            string[] inputNames = new string[] { "In A", "In B" };
            string[] outputNames = new string[] { "Out" };
            Color col = new Color(0.30f, 0.81f, 0.85f);
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

        static ChipDescription CreateTickDelayDescription()
        {
            string name = BuiltinChipNames.TickDelayName;
            string[] inputNames = new string[] { "In" };
            string[] outputNames = new string[] { "Out" };
            Color col = new Color(0, 0, 0);
            return EmptyChipDescriptionCreator.CreateEmptyChipDescription(name, inputNames, outputNames, col);
        }
    }
}
