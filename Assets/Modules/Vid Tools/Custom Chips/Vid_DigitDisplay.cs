using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipCreation;

namespace DLS.VideoTools
{
	// For demoing stuff in video. TODO: read vals directly from simulation state.
	public class Vid_DigitDisplay : MonoBehaviour
	{
		[SerializeField] ChipDisplay chip;
		[SerializeField] MeshRenderer[] segments;
		public Color offCol;
		public Color onCol;

		string[] states = new string[]
		{
			"01111110",
			"00110000",
			"01101101",
			"01111001",
			"00110011",
			"01011011",
			"01011111",
			"01110000",
			"01111111",
			"01111011",
			"11011111",
			"11011011",
			"10110011",
			"11111001",
			"11101101",
			"10110000"
		};


		void Start()
		{
			for (int i = 0; i < segments.Length; i++)
			{
				segments[i].sharedMaterial = Material.Instantiate(segments[i].sharedMaterial);
			}
		}

		void LateUpdate()
		{
			int dec = 0;
			for (int i = 1; i < 5; i++)
			{
				if (chip.InputPins[i].State == Simulation.PinState.HIGH)
				{
					dec |= 1 << (4 - i);
				}


			}

			string valString = states[dec];
			bool blank = chip.InputPins[5].State == Simulation.PinState.HIGH && dec == 0;
			if (blank)
			{
				valString = "00000000";
			}


			//Debug.Log(dec + ": " + valString);
			for (int i = 0; i < segments.Length; i++)
			{
				Color col = (valString[i] == '0') ? offCol : onCol;
				segments[i].sharedMaterial.color = col;
			}

			if (chip.InputPins[0].State == Simulation.PinState.HIGH && !blank)
			{
				segments[0].sharedMaterial.color = onCol;
			}
		}
	}
}