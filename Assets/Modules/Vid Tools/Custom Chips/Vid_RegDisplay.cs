using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebInput;

namespace DLS.ChipCreation
{
	public class Vid_RegDisplay : MonoBehaviour
	{
		[SerializeField] VidRegButton button;
		public TMPro.TMP_Text sText;
		[SerializeField] ChipDisplay chip;
		[SerializeField] MeshRenderer[] segments;
		[SerializeField] MeshRenderer[] segmentsB;
		public Color offCol;
		public Color onCol;

		bool clockOld;
		int memory;

		string states =
@"01111110
00110000
01101101
01111001
00110011
01011011
01011111
01110000
01111111
01111011
11011111
11011011
10110011
11111001
11101101
00000000";


		void Start()
		{
			for (int i = 0; i < segments.Length; i++)
			{
				segments[i].sharedMaterial = Material.Instantiate(segments[i].sharedMaterial);
			}
			for (int i = 0; i < segmentsB.Length; i++)
			{
				segmentsB[i].sharedMaterial = Material.Instantiate(segmentsB[i].sharedMaterial);
			}

			//button.mouseInteraction.MouseEntered += Hello;
		}



		void LateUpdate()
		{
			bool twosComplement = button.on;
			sText.text = twosComplement ? "SIGNED" : "UNSIGNED";
			int dec = 0;
			for (int i = 0; i < 4; i++)
			{
				if (chip.InputPins[i].State == Simulation.PinState.HIGH)
				{
					dec += 1 << (3 - i);
				}
			}

			bool load = chip.InputPins[4].State == Simulation.PinState.HIGH;
			bool clock = chip.InputPins[5].State == Simulation.PinState.HIGH;

			if (clock && clock != clockOld && load)
			{
				memory = dec;
			}
			clockOld = clock;


			Display(memory, twosComplement);


		}

		void Display(int value, bool useTwosComp)
		{
			if (useTwosComp)
			{
				if (value > 8)
				{
					value -= 16;
				}
			}
			int digitTens = value / 10;
			int digitOnes = value % 10;

			Set(segmentsB, digitTens, digitTens == 0);
			Set(segments, digitOnes, false);
		}

		void Set(MeshRenderer[] display, int digit, bool blank)
		{
			string valString = states.Split('\n')[Mathf.Abs(digit)];
			if (blank)
			{
				valString = states.Split('\n')[^1];
			}

			for (int i = 0; i < display.Length; i++)
			{
				Color col = (valString[i] == '0') ? offCol : onCol;
				display[i].sharedMaterial.color = col;
			}
			if (!blank && digit < 0)
			{
				display[0].sharedMaterial.color = onCol;
			}
		}
	}
}