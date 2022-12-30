using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public class SevenSegmentDisplay : MonoBehaviour
	{
		[SerializeField] ChipDisplay chip;
		[SerializeField] MeshRenderer[] segments;
		public Color offCol;
		public Color onCol;
		public Color highlightCol;

		void Start()
		{
			for (int i = 0; i < segments.Length; i++)
			{
				segments[i].sharedMaterial = Material.Instantiate(segments[i].sharedMaterial);
			}
		}

		void LateUpdate()
		{
			for (int i = 0; i < chip.InputPins.Count; i++)
			{
				Color col = chip.InputPins[i].State == Simulation.PinState.HIGH ? onCol : offCol;
				if (chip.InputPins[i].IsHighlighted)
				{
					col = highlightCol;
				}
				segments[i].sharedMaterial.color = col;
			}
		}
	}
}