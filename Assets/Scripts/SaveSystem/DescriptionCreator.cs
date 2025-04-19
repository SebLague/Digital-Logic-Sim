using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.Graphics;
using UnityEngine;
using Random = System.Random;

namespace DLS.SaveSystem
{
	public static class DescriptionCreator
	{
		public static ChipDescription CreateChipDescription(DevChipInstance chip)
		{
			ChipDescription descOld = chip.LastSavedDescription;
			bool hasSavedDesc = descOld != null;
			Vector2 size = hasSavedDesc ? descOld.Size : Vector2.zero;
			Color col = hasSavedDesc ? descOld.Colour : RandomInitialChipColour();
			string name = hasSavedDesc ? descOld.Name : string.Empty;
            string comment = hasSavedDesc ? descOld.ChipComment : string.Empty; 
			DisplayDescription[] displays = hasSavedDesc ? descOld.Displays : null;
            NameDisplayLocation nameLocation = hasSavedDesc ? descOld.NameLocation : NameDisplayLocation.Centre;

            if (UIDrawer.ActiveMenu == UIDrawer.MenuType.ChipCustomization && ChipSaveMenu.ActiveCustomizeChip != null)
            {
                 var customizeDesc = ChipSaveMenu.ActiveCustomizeDescription;
                 name = customizeDesc.Name;
                 size = customizeDesc.Size;
                 col = customizeDesc.Colour;
                 comment = customizeDesc.ChipComment; 
                 displays = customizeDesc.Displays;
                 nameLocation = customizeDesc.NameLocation;
            }

			PinDescription[] inputPins = OrderPins(chip.GetInputPins()).Select(CreatePinDescription).ToArray();
			PinDescription[] outputPins = OrderPins(chip.GetOutputPins()).Select(CreatePinDescription).ToArray();
			SubChipDescription[] subchips = chip.GetSubchips().Select(CreateSubChipDescription).ToArray();
			Vector2 minChipsSize = SubChipInstance.CalculateMinChipSize(inputPins, outputPins, name);
			size = Vector2.Max(minChipsSize, size);

			UpdateWireIndicesForDescriptionCreation(chip);

			return new ChipDescription
			{
				Name = name,
				NameLocation = nameLocation,
				Size = size,
				Colour = col,
                ChipComment = comment, 

				SubChips = subchips,
				InputPins = inputPins,
				OutputPins = outputPins,
				Wires = chip.Wires.Select(CreateWireDescription).ToArray(),
				Displays = displays,
				ChipType = ChipType.Custom
			};
		}

        static IOrderedEnumerable<DevPinInstance> OrderPins(IEnumerable<DevPinInstance> pins)
		{
			return pins.OrderByDescending(p => p.Position.y).ThenBy(p => p.Position.x);
		}


		static SubChipDescription CreateSubChipDescription(SubChipInstance subChip)
		{
			return new SubChipDescription
			(
				subChip.Description.Name,
				subChip.ID,
				subChip.Label,
				subChip.Position,
				subChip.IsBus ? null : subChip.OutputPins.Select(p => new OutputPinColourInfo(p.Colour, p.Address.PinID)).ToArray(),
				subChip.InternalData
			);
		}

		public static SubChipDescription CreateBuiltinSubChipDescriptionForPlacement(ChipType type, string name, int id, Vector2 position)
		{
			uint[] internalData = type switch
			{
				ChipType.Rom_256x16 => new uint[256],
				ChipType.Key => new uint[] { 'K' },
				_ => ChipTypeHelper.IsBusType(type) ? new uint[2] : null
			};

			return new SubChipDescription
			(
				name,
				id,
				string.Empty,
				position,
				Array.Empty<OutputPinColourInfo>(),
				internalData
			);
		}

		static void UpdateWireIndicesForDescriptionCreation(DevChipInstance chip)
		{
			for (int i = 0; i < chip.Wires.Count; i++)
			{
				chip.Wires[i].descriptionCreator_wireIndex = i;
			}
		}

		static WireDescription CreateWireDescription(WireInstance wire)
		{
			Vector2[] wirePoints = new Vector2[wire.WirePointCount];
			for (int i = 0; i < wirePoints.Length; i++)
			{
				if (i == 0 && !wire.SourceConnectionInfo.IsConnectedAtWire) continue;
				if (i == wirePoints.Length - 1 && !wire.TargetConnectionInfo.IsConnectedAtWire) continue;
				wirePoints[i] = wire.GetWirePoint(i);
			}

			WireConnectionType connectionType = WireConnectionType.ToPins;
			int connectedWireIndex = -1;
			int connectedWireSegmentIndex = -1;

			if (wire.ConnectedWire != null)
			{
				if (wire.SourceConnectionInfo.IsConnectedAtWire)
				{
					connectionType = WireConnectionType.ToWireSource;
					connectedWireSegmentIndex = wire.SourceConnectionInfo.wireConnectionSegmentIndex;
				}
				else if (wire.TargetConnectionInfo.IsConnectedAtWire)
				{
					connectionType = WireConnectionType.ToWireTarget;
					connectedWireSegmentIndex = wire.TargetConnectionInfo.wireConnectionSegmentIndex;
				}
				connectedWireIndex = wire.ConnectedWire.descriptionCreator_wireIndex;
			}

			return new WireDescription
			{
				SourcePinAddress = wire.SourcePin.Address,
				TargetPinAddress = wire.TargetPin.Address,
				ConnectionType = connectionType,
				ConnectedWireIndex = connectedWireIndex,
				ConnectedWireSegmentIndex = connectedWireSegmentIndex,
				Points = wirePoints
			};
		}

		public static PinDescription CreatePinDescription(DevPinInstance devPin) =>
			new(
				devPin.Pin.Name,
				devPin.ID,
				devPin.Position,
				devPin.Pin.bitCount,
				devPin.IsInputPin ? devPin.Pin.Colour : default,
				devPin.pinValueDisplayMode
			);

		static Color RandomInitialChipColour()
		{
			Random rng = new();
			float h = (float)rng.NextDouble();
			float s = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
			float v = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
			return Color.HSVToRGB(h, s, v);
		}
	}
}