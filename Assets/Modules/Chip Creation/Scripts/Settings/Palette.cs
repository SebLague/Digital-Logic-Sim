using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.Simulation;
using System.Collections.ObjectModel;

namespace DLS.ChipCreation
{
	[CreateAssetMenu(menuName = "DLS/Palette")]
	public class Palette : ScriptableObject
	{
		public VoltageColour GetDefaultColours() => voltageColours[defaultIndex];
		public ReadOnlyCollection<VoltageColour> Colours => new(voltageColours);


		[SerializeField] int defaultIndex;
		[SerializeField] VoltageColour[] voltageColours;

		public VoltageColour GetTheme(string themeName)
		{
			if (string.IsNullOrEmpty(themeName))
			{
				return voltageColours[0];
			}

			foreach (var theme in voltageColours)
			{
				if (theme.name == themeName)
				{
					return theme;
				}
			}
			Debug.Log("Could not find theme: " + themeName);
			return voltageColours[0];
		}

		[System.Serializable]
		public class VoltageColour
		{
			public string name;
			public Color High;
			public Color Low;
			public int displayPriority;

			public Color GetColour(PinState state, bool useTriStateCol = true)
			{
				switch (state)
				{
					case PinState.HIGH: return High;
					case PinState.LOW: return Low;
					default: return useTriStateCol ? Color.black : Low;
				}
			}
		}
	}
}