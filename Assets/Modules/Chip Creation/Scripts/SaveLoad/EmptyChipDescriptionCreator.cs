using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	public static class EmptyChipDescriptionCreator
	{

		public static ChipDescription CreateEmptyChipDescription(string name)
		{
			return CreateEmptyChipDescription(name, new string[0], new string[0], Color.black);
		}

		public static ChipDescription CreateEmptyChipDescription(string name, string[] inputNames, string[] outputNames, Color col)
		{
			// Create input pins
			PinDescription[] inputPins = new PinDescription[inputNames.Length];
			for (int i = 0; i < inputPins.Length; i++)
			{
				var pin = new PinDescription() { Name = inputNames[i], PositionY = CalculatePinPosition(i, inputPins.Length), ID = i };
				inputPins[i] = pin;
			}

			// Create output pins
			PinDescription[] outputPins = new PinDescription[outputNames.Length];
			for (int i = 0; i < outputPins.Length; i++)
			{
				var pin = new PinDescription() { Name = outputNames[i], PositionY = CalculatePinPosition(i, outputPins.Length), ID = inputPins.Length + i };
				outputPins[i] = pin;
			}

			ChipDescription description = new ChipDescription()
			{
				Name = name,
				Colour = "#" + ColorUtility.ToHtmlStringRGB(col),
				InputPins = inputPins,
				OutputPins = outputPins,
				SubChips = new ChipInstanceData[0],
				Connections = new ConnectionDescription[0]
			};

			return description;
		}

		static float CalculatePinPosition(int i, int num)
		{
			float spacingFactor = 0.75f;
			float t = (num > 1) ? i / (num - 1f) : 0.5f;
			return -(t - 0.5f) * num * spacingFactor;
		}
	}
}