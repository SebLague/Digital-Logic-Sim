using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.SaveSystem;
using UnityEngine;

namespace DLS.Game
{
	public class UndoController
	{
		readonly List<UndoAction> undoHistory = new();
		int undoIndex = -1;
		DevChipInstance devChip;

		public UndoController(DevChipInstance devChip)
		{
			this.devChip = devChip;
		}

		public void TryUndo()
		{
			if (undoIndex == -1) return;

			UndoAction action = undoHistory[undoIndex];
			undoIndex--;

			UndoRedo(action, true);
		}

		public void TryRedo()
		{
			if (undoIndex == undoHistory.Count - 1) return;

			UndoAction action = undoHistory[undoIndex + 1];
			undoIndex++;

			UndoRedo(action, false);
		}

		public void RecordAddWire(WireInstance wire) => RecordAddOrDeleteWire(wire, false);

		public void RecordDeleteWire(WireInstance wire) => RecordAddOrDeleteWire(wire, true);

		void RecordAddOrDeleteWire(WireInstance wire, bool delete)
		{
			DescriptionCreator.UpdateWireIndicesForDescriptionCreation(devChip);
			int wireIndex = wire.descriptionCreator_wireIndex;
			WireDescription wireDesc = DescriptionCreator.CreateWireDescription(wire);

			WireExistenceAction action = new()
			{
				wireDescription = wireDesc,
				wireIndex = wireIndex,
				isDeleteAction = delete
			};

			RecordUndoAction(action);
		}


		public void RecordMoveElements(List<IMoveable> movedElements)
		{
			MoveUndoAction moveUndoAction = new()
			{
				subChipIDs = movedElements.Select(element => element.ID).ToArray(),
				originalPositions = movedElements.Select(element => element.MoveStartPosition).ToArray(),
				newPositions = movedElements.Select(element => element.Position).ToArray(),
			};

			RecordUndoAction(moveUndoAction);
		}

		public void RecordDeleteElements(List<IMoveable> deletedElements)
		{
			RecordAddOrDeleteElements(deletedElements, true);
		}

		public void RecordAddElements(List<IMoveable> addedElements)
		{
			RecordAddOrDeleteElements(addedElements, false);
		}

		void RecordAddOrDeleteElements(List<IMoveable> elements, bool delete)
		{
			List<SubChipInstance> subchips = elements.OfType<SubChipInstance>().ToList();
			DevPinInstance[] devPins = elements.OfType<DevPinInstance>().ToArray();

			// ---- Bus handling ----
			if (!delete)
			{
				// Ignore bus origin when placing (it's not a 'complete' element on its own, it requires the corresponding bus terminus)
				subchips = subchips.Where(s => !ChipTypeHelper.IsBusOriginType(s.ChipType)).ToList();
				if (subchips.Count == 0) return;
			}

			// Ensure that if we have one part of the bus, the linked pair is included as well
			SubChipInstance[] buses = subchips.Where(s => s.IsBus).ToArray();
			if (buses.Length > 0)
			{
				HashSet<int> busIDsInOriginalList = subchips.Where(s => s.IsBus).Select(b => b.ID).ToHashSet();

				foreach (SubChipInstance bus in buses)
				{
					if (busIDsInOriginalList.Contains(bus.LinkedBusPairID)) continue;

					bool foundPair = devChip.TryGetSubChipByID(bus.LinkedBusPairID, out SubChipInstance linkedBus);
					if (!foundPair) throw new Exception("Failed to find bus pair when creating undo/redo action");

					subchips.Add(linkedBus);
				}
			}

			ElementExistenceAction deleteAction = new()
			{
				chipNames = subchips.Select(s => s.Description.Name).ToArray(),
				subchipDescriptions = subchips.Select(DescriptionCreator.CreateSubChipDescription).ToArray(),
				pinDescriptions = devPins.Select(DescriptionCreator.CreatePinDescription).ToArray(),
				pinInInputFlags = devPins.Select(p => p.IsInputPin).ToArray(),
				isDeleteAction = delete
			};

			RecordUndoAction(deleteAction);
		}

		void RecordUndoAction(UndoAction action)
		{
			if (undoIndex != undoHistory.Count)
			{
				undoHistory.RemoveRange(undoIndex + 1, undoHistory.Count - (undoIndex + 1));
			}

			undoHistory.Add(action);
			undoIndex = undoHistory.Count - 1;
		}

		void UndoRedo(UndoAction action, bool undo)
		{
			Project.ActiveProject.controller.CancelEverything();

			try
			{
				if (action is MoveUndoAction move)
				{
					move.Trigger(undo, devChip.Elements);
				}
				else if (action is ElementExistenceAction elementExistence)
				{
					elementExistence.Trigger(undo, devChip);
				}
				else if (action is WireExistenceAction wireExistence)
				{
					wireExistence.Trigger(undo, devChip);
				}
			}
			catch (Exception e)
			{
				// Undo/redo can fail in some cases. For example: player moves chip A, then goes into library and deletes that chip type from the project.
				// Attempting to undo the chip movement will now fail since that chip was forcibly removed. If we encounter such a problem, clear the undo history to prevent potential problems.
				// (should probably think about handling more gracefully in the future though)
				undoHistory.Clear();
				undoIndex = -1;
				if (Application.isEditor) Debug.Log($"Undo/redo action failed. Clearing undo history. Reason: {e.Message} Stack trace: {e.StackTrace}");
			}
		}


		class MoveUndoAction : UndoAction
		{
			public int[] subChipIDs;
			public Vector2[] originalPositions;
			public Vector2[] newPositions;

			public void Trigger(bool undo, List<IMoveable> elements)
			{
				Dictionary<int, IMoveable> elementLookupByID = elements.ToDictionary(element => element.ID, element => element);
				for (int i = 0; i < subChipIDs.Length; i++)
				{
					IMoveable element = elementLookupByID[subChipIDs[i]];
					element.Position = undo ? originalPositions[i] : newPositions[i];
					Project.ActiveProject.controller.Select(element, true);
				}
			}
		}

		class WireExistenceAction : UndoAction
		{
			public WireDescription wireDescription;
			public int wireIndex;
			public bool isDeleteAction;

			public void Trigger(bool undo, DevChipInstance devChip)
			{
				if (!isDeleteAction) undo = !undo;
				bool addWire = undo;

				if (addWire)
				{
					WireInstance connectedWire = wireDescription.ConnectedWireIndex >= 0 ? devChip.Wires[wireDescription.ConnectedWireIndex] : null;
					(WireInstance loadedWire, bool failed) = DevChipInstance.TryLoadWireFromDescription(wireDescription, wireIndex, devChip, connectedWire);
					if (failed) throw new Exception("Failed to load wire in undo/redo action");
					else devChip.AddWire(loadedWire, false, wireIndex);
				}
				else
				{
					devChip.DeleteWire(devChip.Wires[wireIndex]);
				}
			}
		}

		class ElementExistenceAction : UndoAction
		{
			public string[] chipNames;
			public SubChipDescription[] subchipDescriptions;

			public PinDescription[] pinDescriptions;
			public bool[] pinInInputFlags;

			public bool isDeleteAction;

			public void Trigger(bool undo, DevChipInstance devChip)
			{
				if (!isDeleteAction) undo = !undo;
				bool addElement = undo;


				// ---- Handle subchips ----
				for (int i = 0; i < chipNames.Length; i++)
				{
					ChipDescription description = Project.ActiveProject.chipLibrary.GetChipDescription(chipNames[i]);

					if (addElement)
					{
						SubChipInstance subchip = new(description, subchipDescriptions[i]);
						devChip.AddNewSubChip(subchip, false);
						Project.ActiveProject.controller.Select(subchip, true);
					}
					else if (!devChip.TryDeleteSubChipByID(subchipDescriptions[i].ID))
					{
						// (bus pairs deleted automatically, so the other part is expected to fail)
						if (!ChipTypeHelper.IsBusType(description.ChipType)) throw new Exception("Failed to delete subchip");
					}
				}

				// ---- Handle dev pins ----
				for (int i = 0; i < pinDescriptions.Length; i++)
				{
					PinDescription pinDescription = pinDescriptions[i];

					if (addElement)
					{
						DevPinInstance devPin = new(pinDescription, pinInInputFlags[i]);
						devChip.AddNewDevPin(devPin, false);
						Project.ActiveProject.controller.Select(devPin, true);
					}
					else if (!devChip.TryDeleteDevPinByID(pinDescription.ID))
					{
						throw new Exception("Failed to delete dev pin");
					}
				}
			}
		}

		class UndoAction
		{
		}
	}
}