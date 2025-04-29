using DLS.Description;
using DLS.Game;
using UnityEngine;

namespace DLS.SaveSystem
{
	public static class UpgradeHelper
	{
		public static void ApplyVersionChanges(ChipDescription[] customChips, ChipDescription[] builtinChips)
		{
			Main.Version defaultVersion = new(2, 0, 0);
			Main.Version version_2_1_4 = new(2, 1, 4);

			foreach (ChipDescription chipDesc in customChips)
			{
				if (!Main.Version.TryParse(chipDesc.DLSVersion, out Main.Version chipVersion))
				{
					chipVersion = defaultVersion;
				}

				if (chipVersion.ToInt() <= version_2_1_4.ToInt())
				{
					UpdateChipPre_2_1_5(chipDesc);
					chipDesc.DLSVersion = version_2_1_4.ToString();
				}
			}
		}


		static void UpdateChipPre_2_1_5(ChipDescription chipDesc)
		{
			string ledName = ChipTypeHelper.GetName(ChipType.DisplayLED);

			// Update input pin cols
			for (int i = 0; i < chipDesc.InputPins.Length; i++)
			{
				chipDesc.InputPins[i].Colour = GetNewPinColour(chipDesc.InputPins[i].Colour);
			}

			// ---- Added LED colour option (requires instance data array size of 1 for led subchips) ----
			for (int i = 0; i < chipDesc.SubChips.Length; i++)
			{
				SubChipDescription subChipDesc = chipDesc.SubChips[i];

				// Ensure LED type has colour data
				if (ChipDescription.NameMatch(subChipDesc.Name, ledName) && (subChipDesc.InternalData == null || subChipDesc.InternalData.Length == 0))
				{
					chipDesc.SubChips[i].InternalData = DescriptionCreator.CreateDefaultInstanceData(ChipType.DisplayLED);
				}

				// Update subchip output pin cols
				if (subChipDesc.OutputPinColourInfo == null) continue;
				for (int j = 0; j < subChipDesc.OutputPinColourInfo.Length; j++)
				{
					subChipDesc.OutputPinColourInfo[j].PinColour = GetNewPinColour(subChipDesc.OutputPinColourInfo[j].PinColour);
				}
			}

			// ---- Inserted ORANGE as colour option at index 1, so update old indices to correct values ----
			static PinColour GetNewPinColour(PinColour colOld)
			{
				int colourIndex = (int)colOld;
				if (colourIndex > 0) colourIndex++;

				return (PinColour)colourIndex;
			}
		}
	}
}