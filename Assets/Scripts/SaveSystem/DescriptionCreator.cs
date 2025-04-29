using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using UnityEngine;
using Random = System.Random;

namespace DLS.SaveSystem
{
	public static class DescriptionCreator
	{
		public static ChipDescription CreateChipDescription(DevChipInstance chip)
		{
			// Get previously saved customizations such as name and colour (if exist)
			ChipDescription descOld = chip.LastSavedDescription;
			bool hasSavedDesc = descOld != null;
			Vector2 size = hasSavedDesc ? descOld.Size : Vector2.zero;
			Color col = hasSavedDesc ? descOld.Colour : RandomInitialChipColour();
			string name = hasSavedDesc ? descOld.Name : string.Empty;
			DisplayDescription[] displays = hasSavedDesc ? descOld.Displays : null;

			// Create pin and subchip descriptions
			PinDescription[] inputPins = OrderPins(chip.GetInputPins()).Select(CreatePinDescription).ToArray();
			PinDescription[] outputPins = OrderPins(chip.GetOutputPins()).Select(CreatePinDescription).ToArray();
			SubChipDescription[] subchips = chip.GetSubchips().Select(CreateSubChipDescription).ToArray();
			Vector2 minChipsSize = SubChipInstance.CalculateMinChipSize(inputPins, outputPins, name);
			size = Vector2.Max(minChipsSize, size);

			UpdateWireIndicesForDescriptionCreation(chip);

			// Create and return the chip description
			return new ChipDescription
			{
				DLSVersion = Main.DLSVersion.ToString(),
				Name = name,
				NameLocation = hasSavedDesc ? descOld.NameLocation : NameDisplayLocation.Centre,
				Size = size,
				Colour = col,

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


		public static SubChipDescription CreateSubChipDescription(SubChipInstance subChip)
		{
			return new SubChipDescription
			(
				subChip.Description.Name,
				subChip.ID,
				subChip.Label,
				subChip.Position,
				// Don't save colour info for bus since it changes based on received input, so would just trigger unnecessary 'unsaved changes' warnings
				subChip.IsBus ? null : subChip.OutputPins.Select(p => new OutputPinColourInfo(p.Colour, p.Address.PinID)).ToArray(),
				(uint[])subChip.InternalData?.Clone()
			);
		}

		public static SubChipDescription CreateBuiltinSubChipDescriptionForPlacement(ChipType type, string name, int id, Vector2 position)
		{
			return new SubChipDescription
			(
				name,
				id,
				string.Empty,
				position,
				Array.Empty<OutputPinColourInfo>(),
				CreateDefaultInstanceData(type)
			);
		}

		public static uint[] CreateDefaultInstanceData(ChipType type)
		{
			return type switch
			{
				ChipType.Rom_256x16 => new uint[256], // ROM contents
				ChipType.Key => new uint[] { 'K' }, // Key binding
				ChipType.Pulse => new uint[] { 50, 0, 0 }, // Pulse width, ticks remaining, input state old
				ChipType.DisplayLED => new uint[] { 0 }, // LED colour
				_ => ChipTypeHelper.IsBusType(type) ? new uint[2] : null
			};
		}

		public static void UpdateWireIndicesForDescriptionCreation(DevChipInstance chip)
		{
			// Store wire's current index in wire for convenient access
			for (int i = 0; i < chip.Wires.Count; i++)
			{
				chip.Wires[i].descriptionCreator_wireIndex = i;
			}
		}

		// Note: assumed that all wire indices have been set prior to calling this function
		public static WireDescription CreateWireDescription(WireInstance wire)
		{
			// Get wire points
			Vector2[] wirePoints = new Vector2[wire.WirePointCount];
			for (int i = 0; i < wirePoints.Length; i++)
			{
				// Don't need to save start/end points (just leave as zero) since they get their positions from the pins they're connected to (unless starting/ending at another wire).
				// Benefit of leaving zero is that if a subchip is opened and modified (for example a pin is added, so pin spacing changes), then when opening this chip again, it won't
				// immediately think it has unsaved changes (and unnecessarily notify the player), just because the start/end points of wires going to those modified pins has changed.
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
				// Don't save colour info for output pin since it changes based on received input, so would just trigger unecessary 'unsaved changes' warnings
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