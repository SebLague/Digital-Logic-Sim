using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Linq;

namespace DLS.ChipCreation
{
	public static class ChipDescriptionCreator
	{

		public static ChipDescription CreateChipDescription(ChipEditor chipEditor)
		{
			PinDescription[] inputPins = CreatePinDescriptions(chipEditor.PinPlacer.InputPins);
			PinDescription[] outputPins = CreatePinDescriptions(chipEditor.PinPlacer.OutputPins);
			ChipInstanceData[] subChipDescriptions = CreateSubChipDescriptions(chipEditor.AllSubChips);
			ConnectionDescription[] connectionDescriptions = CreateConnectionDescriptions(chipEditor);

			ChipDescription chipDescription = new ChipDescription()
			{
				Name = chipEditor.LastSavedDescription.Name,
				Colour = chipEditor.LastSavedDescription.Colour,
				InputPins = inputPins,
				OutputPins = outputPins,
				SubChips = subChipDescriptions,
				Connections = connectionDescriptions
			};

			return chipDescription;
		}

		// Create sub-chip description array
		static ChipInstanceData[] CreateSubChipDescriptions(IList<ChipBase> subChips)
		{
			return subChips.Select(chip => chip.GetInstanceData()).ToArray();
		}

		// Create pin description array
		static PinDescription[] CreatePinDescriptions(IList<EditablePin> pins)
		{
			List<PinDescription> descriptions = pins.Select(pin => CreateDescription(pin)).ToList();
			return descriptions.ToArray();

			PinDescription CreateDescription(EditablePin pin)
			{
				return new PinDescription()
				{
					Name = pin.PinName,
					ID = pin.GetPin().ID,
					PositionY = pin.transform.position.y,
					ColourThemeName = pin.GetPin().ColourTheme.name
				};
			}
		}

		static ConnectionDescription[] CreateConnectionDescriptions(ChipEditor chipEditor)
		{
			// Don't save bus wires, these are handled inside the bus 'chip'
			var wiresToSave = chipEditor.WireEditor.AllWires.Where(w => !w.IsBusWire);
			return wiresToSave.Select(wire => GetConnectionFromWire(wire)).ToArray();

			ConnectionDescription GetConnectionFromWire(Wire wire)
			{
				return new ConnectionDescription()
				{
					Source = chipEditor.GetPinAddress(wire.SourcePin),
					Target = chipEditor.GetPinAddress(wire.TargetPin),
					WirePoints = wire.AnchorPoints.Select(p => new Point(p.x, p.y)).ToArray(),
					ColourThemeName = wire.ColourTheme.name
				};
			}
		}

	}
}
