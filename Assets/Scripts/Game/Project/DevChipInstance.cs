using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.SaveSystem;
using DLS.Simulation;
using UnityEngine;

namespace DLS.Game
{
	public class DevChipInstance
	{
		public readonly List<IMoveable> Elements = new();
		public readonly List<WireInstance> Wires = new();

		public readonly UndoController UndoController;

		// Names of all the chips which contain this chip (either directly, or inside some other subchip)
		public readonly HashSet<string> AllParentChipNames = new(ChipDescription.NameComparer);

		public ChipDescription LastSavedDescription;
		DevPinInstance[] inputPins_cached = Array.Empty<DevPinInstance>();
		bool elementsModifiedSinceLastArrayUpdate;

		public SimChip SimChip;
		bool hasSimChip;

		public string ChipName => LastSavedDescription == null ? string.Empty : LastSavedDescription.Name;

		public DevChipInstance()
		{
			UndoController = new UndoController(this);
		}

		public void SetSimChip(SimChip simChip)
		{
			hasSimChip = true;
			SimChip = simChip;
		}

		public void RebuildSimulation()
		{
			ChipDescription desc = DescriptionCreator.CreateChipDescription(this);
			SimChip simChip = Simulator.BuildSimChip(desc, Project.ActiveProject.chipLibrary);
			SetSimChip(simChip);
		}


		public DevPinInstance[] GetInputPins()
		{
			if (elementsModifiedSinceLastArrayUpdate)
			{
				elementsModifiedSinceLastArrayUpdate = false;
				inputPins_cached = Elements.OfType<DevPinInstance>().Where(p => p.IsInputPin).ToArray();
			}

			return inputPins_cached;
		}

		public static (DevChipInstance devChip, bool anyElementFailedToLoad) LoadFromDescriptionTest(ChipDescription description, ChipLibrary library)
		{
			DevChipInstance instance = new();
			instance.LastSavedDescription = description;

			// Set any null arrays to empty so don't have to check
			description.SubChips ??= Array.Empty<SubChipDescription>();
			description.InputPins ??= Array.Empty<PinDescription>();
			description.OutputPins ??= Array.Empty<PinDescription>();
			description.Wires ??= Array.Empty<WireDescription>();

			bool anyElementFailedToLoad = false;

			// Load subchips
			foreach (SubChipDescription subChipDescription in description.SubChips)
			{
				if (library.TryGetChipDescription(subChipDescription.Name, out ChipDescription fullDescriptionOfSubchip))
				{
					SubChipInstance subChip = new(fullDescriptionOfSubchip, subChipDescription);
					instance.AddNewSubChip(subChip, true);
				}
				else anyElementFailedToLoad = true;
			}

			// Load dev pins
			for (int i = 0; i < description.InputPins.Length; i++)
			{
				PinDescription pinDescription = description.InputPins[i];
				instance.AddNewDevPin(new DevPinInstance(pinDescription, true), true);
			}

			for (int i = 0; i < description.OutputPins.Length; i++)
			{
				PinDescription pinDescription = description.OutputPins[i];
				instance.AddNewDevPin(new DevPinInstance(pinDescription, false), true);
			}

			// ---- Load wires ----
			// Wires can fail to load if associated pin was deleted from subchip. This means that wire indices stored in the save data might not line up with our loaded wires list.
			// So, keep track here of the wires with correct indices (with failed entries just being left as null)
			WireInstance[] loadedWiresWithOriginalIndices = new WireInstance[description.Wires.Length];

			for (int i = 0; i < description.Wires.Length; i++)
			{
				WireDescription wireDescription = description.Wires[i];
				(WireInstance loadedWire, bool failed) = TryLoadWireFromDescription(wireDescription, i, instance, loadedWiresWithOriginalIndices);

				if (!failed) instance.AddWire(loadedWire, true);

				loadedWiresWithOriginalIndices[i] = loadedWire;
				anyElementFailedToLoad |= failed;
			}

			instance.RegenerateParentChipNamesHash();

			return (instance, anyElementFailedToLoad);
		}

		public static (WireInstance loadedWire, bool failed) TryLoadWireFromDescription(WireDescription wireDescription, int wireIndex, DevChipInstance instance, IList<WireInstance> allWires)
		{
			bool failedToLoad = false;
			WireInstance loadedWire = null;
			WireInstance connectedWire = wireDescription.ConnectedWireIndex >= 0 ? allWires[wireDescription.ConnectedWireIndex] : null;

			instance.TryFindPin(wireDescription.SourcePinAddress, out PinInstance sourcePin);
			instance.TryFindPin(wireDescription.TargetPinAddress, out PinInstance targetPin);

			if (sourcePin != null && targetPin != null)
			{
				WireConnectionType connectionType = wireDescription.ConnectionType;

				if (connectionType is WireConnectionType.ToWireSource or WireConnectionType.ToWireTarget)
				{
					WireInstance wireConnectTarget = connectedWire;
					bool wireConnectTargetFailedToLoad = wireConnectTarget == null;

					if (!wireConnectTargetFailedToLoad)
					{
						// If wire connection target did load, double check that it connects to the same pin that this wire is expecting
						// (this should always be the case, but a bug in a previous version could cause save files to contain bad connection data)
						bool addressMismatch = false;
						addressMismatch |= connectionType is WireConnectionType.ToWireSource && !PinAddress.Equals(wireConnectTarget.SourcePin.Address, wireDescription.SourcePinAddress);
						addressMismatch |= connectionType is WireConnectionType.ToWireTarget && !PinAddress.Equals(wireConnectTarget.TargetPin_BusCorrected.Address, wireDescription.TargetPinAddress);
						wireConnectTargetFailedToLoad = addressMismatch;
					}

					// If wire is connected to another wire, but the other wire failed to load, then fallback to pin connection type 
					// (Load failure could be due to a pin could being deleted from a subchip, or the whole subchip being deleted from the library for example)
					if (wireConnectTargetFailedToLoad)
					{
						failedToLoad = true;
						connectionType = WireConnectionType.ToPins;
					}
				}

				WireInstance.ConnectionInfo sourceConnection = new()
				{
					pin = sourcePin,
					connectedWire = connectionType == WireConnectionType.ToWireSource ? connectedWire : null,
					wireConnectionSegmentIndex = wireDescription.ConnectedWireSegmentIndex
				};

				WireInstance.ConnectionInfo targetConnection = new()
				{
					pin = targetPin,
					connectedWire = connectionType == WireConnectionType.ToWireTarget ? connectedWire : null,
					wireConnectionSegmentIndex = wireDescription.ConnectedWireSegmentIndex
				};

				loadedWire = new WireInstance(sourceConnection, targetConnection, wireDescription.Points, wireIndex);
			}
			else
			{
				failedToLoad = true;
			}

			return (loadedWire, failedToLoad);
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

		public void DeleteSubchipsByName(string removeName)
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
				Simulator.AddSubChip(SimChip, subChip.Description, Project.ActiveProject.chipLibrary, subChip.ID, subChip.InternalData);
			}
		}

		public void AddNewDevPin(DevPinInstance pin, bool isLoadingFromFile)
		{
			AddElement(pin);
			if (!isLoadingFromFile)
			{
				Simulator.AddPin(SimChip, pin.ID, pin.IsInputPin);
			}
		}

		public void AddWire(WireInstance wire, bool isLoading, int insertIndex = -1)
		{
			bool insert = insertIndex != -1;
			if (insert) Wires.Insert(insertIndex, wire);
			else Wires.Add(wire);

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
			elementsModifiedSinceLastArrayUpdate = true;
			bool success = Elements.Remove(element);
			Debug.Assert(success, "Trying to delete element that was already deleted?");
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
			if (hasSimChip)
			{
				Simulator.RemoveConnection(SimChip, wireToDelete.SourcePin.Address, wireToDelete.TargetPin.Address);
			}

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


		public bool DeleteWiresAttachedToPinOfSubChip(int pinID)
		{
			bool anyDeleted = false;

			for (int i = Wires.Count - 1; i >= 0; i--)
			{
				WireInstance wire = Wires[i];
				bool sourceMatch = wire.SourcePin.parent is SubChipInstance && wire.SourcePin.Address.PinID == pinID;
				bool targetMatch = wire.TargetPin.parent is SubChipInstance && wire.TargetPin.Address.PinID == pinID;

				if (sourceMatch || targetMatch)
				{
					DeleteWire(wire);
					anyDeleted = true;
				}
			}

			return anyDeleted;
		}

		public void GetWiresAttachedToElement(int elementID, HashSet<WireInstance> set)
		{
			foreach (WireInstance wire in Wires)
			{
				bool sourceMatch = wire.SourcePin.Address.PinOwnerID == elementID;
				bool targetMatch = wire.TargetPin.Address.PinOwnerID == elementID;

				if (sourceMatch || targetMatch)
				{
					set.Add(wire);
				}
			}
		}

		public void DeleteWiresAttachedToElement(int elementID)
		{
			for (int i = Wires.Count - 1; i >= 0; i--)
			{
				WireInstance wire = Wires[i];
				bool sourceMatch = wire.SourcePin.Address.PinOwnerID == elementID;
				bool targetMatch = wire.TargetPin.Address.PinOwnerID == elementID;

				if (sourceMatch || targetMatch)
				{
					DeleteWire(wire);
				}
			}
		}


		public void DeleteSubChip(SubChipInstance subChip)
		{
			DeleteWiresAttachedToElement(subChip.ID);
			RemoveElement(subChip);

			if (hasSimChip) Simulator.RemoveSubChip(SimChip, subChip.ID);
		}

		// Delete subchip with given id (if it exists)
		public bool TryDeleteSubChipByID(int id)
		{
			for (int i = 0; i < Elements.Count; i++)
			{
				if (Elements[i] is SubChipInstance subchip && subchip.ID == id)
				{
					DeleteSubChip(subchip);
					return true;
				}
			}

			return false;
		}

		// Delete devpin with given id (if it exists)
		public bool TryDeleteDevPinByID(int id)
		{
			for (int i = 0; i < Elements.Count; i++)
			{
				if (Elements[i] is DevPinInstance devPin && devPin.ID == id)
				{
					DeleteDevPin(devPin);
					return true;
				}
			}

			return false;
		}

		public bool TryGetSubChipByID(int id, out SubChipInstance subchip)
		{
			foreach (IMoveable element in Elements)
			{
				if (element.ID == id)
				{
					subchip = (SubChipInstance)element;
					return true;
				}
			}

			subchip = null;
			return false;
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
						devPin.Pin.State = simPin.State;

						if (devPin.IsInputPin || simPin.latestSourceID == -1) continue;

						// Output pins get colour from whichever pin they last received a signal
						PinInstance colSource = TryFindPinFromSimPinSource(simChip, simPin);
						devPin.Pin.Colour = colSource.Colour;
					}
					// -- Subchip --
					else if (element is SubChipInstance subChip)
					{
						// Update the state of each output pin on the subchip to match the state of corresponding pin in the simulation
						foreach (PinInstance subChipOutputPin in subChip.OutputPins)
						{
							SimPin simPin = simChip.GetSimPinFromAddress(subChipOutputPin.Address);
							subChipOutputPin.State = simPin.State;

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

		public bool TryFindPin(PinAddress address, out PinInstance pinInstance) => TryFindPin(Elements, address, out pinInstance);

		public static bool TryFindPin(List<IMoveable> elements, PinAddress address, out PinInstance pinInstance)
		{
			foreach (IMoveable element in elements)
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