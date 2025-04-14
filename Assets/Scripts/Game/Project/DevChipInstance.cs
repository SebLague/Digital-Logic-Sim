using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Simulation;
using UnityEngine;

namespace DLS.Game
{
	public class DevChipInstance
	{
		// Names of all the chips which contain this chip (either directly, or inside some other subchip)
		public readonly HashSet<string> AllParentChipNames = new(ChipDescription.NameComparer);
		public readonly List<IMoveable> Elements = new();
		public readonly SimChip SimChip;
		public readonly List<WireInstance> Wires = new();

		bool elementsModifiedSinceLastArrayUpdate;
		DevPinInstance[] inputPins_cached = Array.Empty<DevPinInstance>();
		public ChipDescription LastSavedDescription;

		public DevChipInstance(ChipDescription description, ChipLibrary chipLibrary, SimChip simChip)
		{
			SimChip = simChip;

			LoadFromDescription(description, chipLibrary);
			RegenerateParentChipNamesHash();
		}

		public DevChipInstance()
		{
			SimChip = new SimChip();
		}

		public string ChipName => LastSavedDescription == null ? string.Empty : LastSavedDescription.Name;

		public DevPinInstance[] GetInputPins()
		{
			if (elementsModifiedSinceLastArrayUpdate)
			{
				elementsModifiedSinceLastArrayUpdate = false;
				inputPins_cached = Elements.OfType<DevPinInstance>().Where(p => p.IsInputPin).ToArray();
			}

			return inputPins_cached;
		}

		void LoadFromDescription(ChipDescription description, ChipLibrary library)
		{
			LastSavedDescription = description;

			// Set any null arrays to empty so don't have to check
			description.SubChips ??= Array.Empty<SubChipDescription>();
			description.InputPins ??= Array.Empty<PinDescription>();
			description.OutputPins ??= Array.Empty<PinDescription>();
			description.Wires ??= Array.Empty<WireDescription>();

			// Load subchips
			foreach (SubChipDescription subChipDescription in description.SubChips)
			{
				if (library.TryGetChipDescription(subChipDescription.Name, out ChipDescription fullDescriptionOfSubchip))
				{
					SubChipInstance subChip = new(fullDescriptionOfSubchip, subChipDescription);
					AddNewSubChip(subChip, true);
				}
			}

			// Load dev pins
			for (int i = 0; i < description.InputPins.Length; i++)
			{
				PinDescription pinDescription = description.InputPins[i];
				AddNewDevPin(new DevPinInstance(pinDescription, true), true);
			}

			for (int i = 0; i < description.OutputPins.Length; i++)
			{
				PinDescription pinDescription = description.OutputPins[i];
				AddNewDevPin(new DevPinInstance(pinDescription, false), true);
			}

			// ---- Load wires ----
			// Wires can fail to load if associated pin was deleted from subchip. This means that wire indices stored in the save data might not line up with our loaded wires list.
			// So, keep track here of the wires with correct indices (with failed entries just being left as null)
			WireInstance[] loadedWiresWithOriginalIndices = new WireInstance[description.Wires.Length];
			int wireIndex = 0;

			for (int i = 0; i < description.Wires.Length; i++)
			{
				WireDescription wireDescription = description.Wires[i];
				TryFindPin(wireDescription.SourcePinAddress, out PinInstance sourcePin);
				TryFindPin(wireDescription.TargetPinAddress, out PinInstance targetPin);

				if (sourcePin != null && targetPin != null)
				{
					WireConnectionType connectionType = wireDescription.ConnectionType;

					// If wire is connected to another wire, but the other wire failed to load (pin could be deleted from a subchip), then fallback to pin connection type
					if (connectionType is WireConnectionType.ToWireSource or WireConnectionType.ToWireTarget && loadedWiresWithOriginalIndices[wireDescription.ConnectedWireIndex] == null)
					{
						connectionType = WireConnectionType.ToPins;
					}

					WireInstance.ConnectionInfo sourceConnection = new()
					{
						pin = sourcePin,
						connectedWire = connectionType == WireConnectionType.ToWireSource ? loadedWiresWithOriginalIndices[wireDescription.ConnectedWireIndex] : null,
						wireConnectionSegmentIndex = wireDescription.ConnectedWireSegmentIndex
					};

					WireInstance.ConnectionInfo targetConnection = new()
					{
						pin = targetPin,
						connectedWire = connectionType == WireConnectionType.ToWireTarget ? loadedWiresWithOriginalIndices[wireDescription.ConnectedWireIndex] : null,
						wireConnectionSegmentIndex = wireDescription.ConnectedWireSegmentIndex
					};

					WireInstance loadedWire = new(sourceConnection, targetConnection, wireDescription.Points, i);
					AddWire(loadedWire, true);
					loadedWiresWithOriginalIndices[wireIndex] = loadedWire;
				}

				wireIndex++;
			}
		}

		// Check if subchip can be added
		// The chip may not contain a copy of itself (either directly, or inside some other chip)
		public bool CanAddSubchip(string subchipName)
		{
			// Check if the chip we want to add is the chip itself
			if (ChipDescription.NameMatch(ChipName, subchipName)) return false;
			// Check if the chip we want to add contains this chip
			return !AllParentChipNames.Contains(subchipName);
		}

		public void RemoveSubchipsByName(string removeName)
		{
			List<SubChipInstance> subchipsToDelete = new();

			foreach (IMoveable element in Elements)
			{
				if (element is SubChipInstance subchip)
				{
					if (subchip.Description.NameMatch(removeName))
					{
						subchipsToDelete.Add(subchip);
					}
				}
			}

			foreach (SubChipInstance subchip in subchipsToDelete)
			{
				DeleteSubChip(subchip);
			}
		}

		public void NotifySaved(ChipDescription savedDescription)
		{
			LastSavedDescription = savedDescription;

			RegenerateParentChipNamesHash();
		}

		public void AddNewSubChip(SubChipInstance subChip, bool isLoading)
		{
			AddElement(subChip);
			if (!isLoading)
			{
				Simulator.AddSubChip(SimChip, subChip.Description, Project.ActiveProject.chipLibrary, subChip.InitialSubChipDesc);
			}
		}

		public void AddNewDevPin(DevPinInstance pin, bool isLoadingFromFile)
		{
			AddElement(pin);
			if (!isLoadingFromFile)
			{
				Simulator.AddPin(SimChip, pin.ID, pin.BitCount, pin.IsInputPin);
			}
		}

		public void AddWire(WireInstance wire, bool isLoading)
		{
			Wires.Add(wire);
			if (!isLoading)
			{
				Simulator.AddConnection(SimChip, wire.SourcePin.Address, wire.TargetPin.Address);
			}
		}

		void AddElement(IMoveable element)
		{
			Elements.Add(element);
			elementsModifiedSinceLastArrayUpdate = true;
		}

		void RemoveElement(IMoveable element)
		{
			Elements.Remove(element);
			elementsModifiedSinceLastArrayUpdate = true;
		}


		public void DeleteDevPin(DevPinInstance devPin)
		{
			DeleteWiresAttachedToPin(devPin.Pin);
			RemoveElement(devPin);
			Simulator.RemovePin(SimChip, devPin.ID);
		}

		public void DeleteWire(WireInstance wireToDelete)
		{
			bool success = Wires.Remove(wireToDelete);
			if (!success) return; // Wire already deleted

			// Remove from simulation
			Simulator.RemoveConnection(SimChip, wireToDelete.SourcePin.Address, wireToDelete.TargetPin.Address);

			// If deleting bus line, automatically delete all other connecting wires
			if (wireToDelete.IsBusWire)
			{
				SubChipInstance busSource = (SubChipInstance)wireToDelete.SourcePin.parent;
				DeleteWiresAttachedToPin(busSource.InputPins[0]);
				DeleteWiresAttachedToPin(busSource.OutputPins[0]);
			}
			// Otherwise connecting wires should connect directly to the pin where the deleted wire used to connect
			else
			{
				foreach (WireInstance other in Wires)
				{
					if (other.ConnectedWire == wireToDelete)
					{
						other.RemoveConnectionDependency();
					}
				}
			}
		}


		void DeleteWiresAttachedToPins(PinInstance[] pins)
		{
			foreach (PinInstance pin in pins)
			{
				DeleteWiresAttachedToPin(pin);
			}
		}

		void DeleteWiresAttachedToPin(PinInstance pin)
		{
			for (int i = Wires.Count - 1; i >= 0; i--)
			{
				WireInstance wire = Wires[i];
				// Check if wire is connected to the deleted pin
				if (PinAddress.Equals(wire.SourcePin.Address, pin.Address) || PinAddress.Equals(wire.TargetPin.Address, pin.Address))
				{
					DeleteWire(wire);
				}
			}
		}


		public void DeleteSubChip(SubChipInstance subChip)
		{
			// Ensure subchip exists before deleting
			// (required for buses, where one end of bus is deleted automatically when other end is deleted; but user may select both ends for deletion)
			if (!Elements.Contains(subChip)) return;

			DeleteWiresAttachedToPins(subChip.AllPins);
			RemoveElement(subChip);
			Simulator.RemoveSubChip(SimChip, subChip.ID);

			// If deleting bus origin/terminus, delete the corresponding terminus/origin
			if (subChip.IsBus)
			{
				TryDeleteSubChipByID(subChip.LinkedBusPairID);
			}
		}

		// Delete subchip with given id (if it exists)
		public void TryDeleteSubChipByID(int id)
		{
			for (int i = 0; i < Elements.Count; i++)
			{
				if (Elements[i] is SubChipInstance subchip && subchip.ID == id)
				{
					DeleteSubChip(subchip);
					return;
				}
			}
		}

		// Update the currently viewed chip from the state of the corresponding simChip.
		// Optionally don't set input pins since player controls these (at least when editing a chip, rather than viewing it)
		public void UpdateStateFromSim(SimChip simChip, bool updateInputPins)
		{
			try
			{
				foreach (IMoveable element in Elements)
				{
					if (element is DevPinInstance devPin)
					{
						if (devPin.IsInputPin && !updateInputPins) continue;

						SimPin simPin = simChip.GetSimPinFromAddress(devPin.Pin.Address);
						devPin.Pin.State.SetFromSource(simPin.State);

						if (devPin.IsInputPin || simPin.latestSourceID == -1) continue;

						// Output pins get colour from whichever pin they last received a signal
						PinInstance colSource = TryFindPinFromSimPinSource(simChip, simPin);
						devPin.Pin.Colour = colSource.Colour;
					}
					// -- Subchip --
					else if (element is SubChipInstance subChip)
					{
						// Update the state of each outpin pin on the subchip to match the state of corresponding pin in the simulation
						foreach (PinInstance subChipOutputPin in subChip.OutputPins)
						{
							SimPin simPin = simChip.GetSimPinFromAddress(subChipOutputPin.Address);
							subChipOutputPin.State.SetFromSource(simPin.State);

							// If is bus, copy colour from the input source
							if (ChipTypeHelper.IsBusOriginType(subChip.ChipType))
							{
								SimPin simInputPin = simChip.GetSimPinFromAddress(subChip.InputPins[0].Address);
								if (simInputPin.latestSourceID == -1) continue;
								PinInstance colSource = TryFindPinFromSimPinSource(simChip, simInputPin);
								subChipOutputPin.Colour = colSource.Colour;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				// Updating from sim thread so it's possible for some stuff to be out of sync if player is actively editing -- just ignore
				if (Application.isEditor)
				{
					Debug.Log("Ignoring exception when updating display from sim state: " + e.Message + "\nStack Trace: " + e.StackTrace);
				}
			}
		}

		PinInstance TryFindPinFromSimPinSource(SimChip simChip, SimPin simPin)
		{
			PinAddress srcAddress;
			// Is dev pin, so here the SimPin's ID refers to the devpin
			if (simPin.latestSourceParentChipID == simChip.ID) srcAddress = new PinAddress(simPin.latestSourceID, 0);
			else srcAddress = new PinAddress(simPin.latestSourceParentChipID, simPin.latestSourceID); // subchip pin

			if (TryFindPin(srcAddress, out PinInstance colSource)) return colSource;
			throw new Exception($"Failed to find colour source: pinID: {srcAddress.PinID}  pinOwnerID: {srcAddress.PinOwnerID}");
		}

		public bool TryFindPin(PinAddress address, out PinInstance pinInstance)
		{
			foreach (IMoveable element in Elements)
			{
				if (element.ID == address.PinOwnerID)
				{
					if (element is SubChipInstance subchip)
					{
						foreach (PinInstance pin in subchip.AllPins)
						{
							if (PinAddress.Equals(pin.Address, address))
							{
								pinInstance = pin;
								return true;
							}
						}
					}
					else if (element is DevPinInstance devPin)
					{
						if (PinAddress.Equals(devPin.Pin.Address, address))
						{
							pinInstance = devPin.Pin;
							return true;
						}
					}

					break;
				}
			}


			pinInstance = null;
			return false;
		}

		public void NotifyConnectedWiresPointsInserted(WireInstance wire, int insertIndex, float insertPointT, int numPoints)
		{
			foreach (WireInstance other in Wires)
			{
				if (other.ConnectedWire == wire)
				{
					other.NotifyParentWirePointsInserted(insertIndex, insertPointT, numPoints);
				}
			}
		}

		public IEnumerable<SubChipInstance> GetSubchips() => Elements.OfType<SubChipInstance>();

		public IEnumerable<DevPinInstance> GetOutputPins()
		{
			return Elements.OfType<DevPinInstance>().Where(p => !p.IsInputPin);
		}

		void RegenerateParentChipNamesHash()
		{
			AllParentChipNames.Clear();
			GetAllParentChipNames(LastSavedDescription.Name, Project.ActiveProject.chipLibrary, AllParentChipNames);
		}

		// Recursively get the names of all the chips which contain this chip (either directly, or inside of some other subchip)
		// (and add to the hashset)
		void GetAllParentChipNames(string name, ChipLibrary library, HashSet<string> hashset)
		{
			RecursivelyAddParents(name);

			void RecursivelyAddParents(string name)
			{
				ChipDescription[] directParents = library.GetDirectParentChips(name);
				foreach (ChipDescription parent in directParents)
				{
					if (hashset.Add(parent.Name))
					{
						RecursivelyAddParents(parent.Name);
					}
				}
			}
		}
	}
}