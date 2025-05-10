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
		readonly DevChipInstance devChip;

		public UndoController(DevChipInstance devChip)
		{
			this.devChip = devChip;
		}

		public void Clear()
		{
			undoHistory.Clear();
			undoIndex = -1;
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

			FullWireState stateBeforeDelete = null;
			if (delete) stateBeforeDelete = CreateFullWireState(devChip, new HashSet<WireInstance>(new[] { wire }));

			WireExistenceAction action = new()
			{
				wireDescription = wireDesc,
				wireIndex = wireIndex,
				fullWireStateBeforeDelete = stateBeforeDelete,
				isDeleteAction = delete
			};

			RecordUndoAction(action);
		}


		public void RecordMoveElements(List<IMoveable> movedElements)
		{
			int count = movedElements.Count;
			MoveUndoAction moveUndoAction = new()
			{
				subChipIDs = new int[count],
				originalPositions = new Vector2[count],
				newPositions = new Vector2[count]
			};

			// Selected elements
			for (int i = 0; i < count; i++)
			{
				IMoveable element = movedElements[i];
				moveUndoAction.subChipIDs[i] = element.ID;
				moveUndoAction.originalPositions[i] = element.MoveStartPosition;
				moveUndoAction.newPositions[i] = element.Position;
			}

			// Wire move offsets
			int wireCount = devChip.Wires.Count;
			moveUndoAction.wireMoveOffsets = new Vector2[wireCount];
			for (int i = 0; i < wireCount; i++)
			{
				moveUndoAction.wireMoveOffsets[i] = devChip.Wires[i].MoveOffset;
			}

			RecordUndoAction(moveUndoAction);
		}

		public void RecordDeleteElements(List<IMoveable> deletedElements)
		{
			bool hasConnectedWires = true; // Todo: test if true so don't backup wire state unnecessarily
			RecordAddOrDeleteElements(deletedElements, true, hasConnectedWires);
		}

		public void RecordAddElements(List<IMoveable> addedElements, bool hasWires)
		{
			RecordAddOrDeleteElements(addedElements, false, hasWires);
		}

		void RecordAddOrDeleteElements(List<IMoveable> elements, bool delete, bool hasWires)
		{
			List<SubChipInstance> subchips = elements.OfType<SubChipInstance>().ToList();
			DevPinInstance[] devPins = elements.OfType<DevPinInstance>().ToArray();

			// When deleting elements, store full state of ALL wires, not just those affected by the deletion.
			// This is because other wires may be connected to the deleted wires (in which case their points are modified),
			// and we want their original state to be restored as well. It's a bit of a pain to specially handle those, so just do a full state backup.
			// Note: also store wire state when duplicating elements, as any connected wires are duplicated and placed along with them.
			FullWireState wireState = null;
			if (hasWires)
			{
				HashSet<WireInstance> wiresThatWillBeDeletedAutomatically = new();
				foreach (IMoveable element in elements)
				{
					devChip.GetWiresAttachedToElement(element.ID, wiresThatWillBeDeletedAutomatically);
				}

				wireState = CreateFullWireState(devChip, wiresThatWillBeDeletedAutomatically);
			}

			ElementExistenceAction deleteAction = new()
			{
				chipNames = subchips.Select(s => s.Description.Name).ToArray(),
				subchipDescriptions = subchips.Select(DescriptionCreator.CreateSubChipDescription).ToArray(),
				pinDescriptions = devPins.Select(DescriptionCreator.CreatePinDescription).ToArray(),
				pinInInputFlags = devPins.Select(p => p.IsInputPin).ToArray(),
				wireStateBeforeDelete = wireState,
				isDeleteAction = delete
			};

			RecordUndoAction(deleteAction);
		}

		static FullWireState CreateFullWireState(DevChipInstance devChip, HashSet<WireInstance> wiresThatWillBeDeleted)
		{
			DescriptionCreator.UpdateWireIndicesForDescriptionCreation(devChip);
			WireDescription[] wireDescriptions = new WireDescription[devChip.Wires.Count];
			bool[] willDeleteWireFlags = new bool[devChip.Wires.Count];

			for (int i = 0; i < wireDescriptions.Length; i++)
			{
				wireDescriptions[i] = DescriptionCreator.CreateWireDescription(devChip.Wires[i]);
				willDeleteWireFlags[i] = wiresThatWillBeDeleted.Contains(devChip.Wires[i]);
				//Debug.Log($"Full wire state: {i}  Will delete: {willDeleteWireFlags[i]}");
			}

			FullWireState wireStateBeforeDelete = new()
			{
				wireDescriptions = wireDescriptions,
				createFlags = willDeleteWireFlags,
			};

			return wireStateBeforeDelete;
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
					move.Trigger(undo, devChip);
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
				if (Application.isEditor) Debug.Log($"Undo/redo action failed. Reason: {e.Message} Stack trace: {e.StackTrace}");
			}
		}


		class MoveUndoAction : UndoAction
		{
			public int[] subChipIDs;
			public Vector2[] originalPositions;
			public Vector2[] newPositions;
			public Vector2[] wireMoveOffsets;


			public void Trigger(bool undo, DevChipInstance devChip)
			{
				Dictionary<int, IMoveable> elementLookupByID = devChip.Elements.ToDictionary(element => element.ID, element => element);
				for (int i = 0; i < subChipIDs.Length; i++)
				{
					IMoveable element = elementLookupByID[subChipIDs[i]];
					element.Position = undo ? originalPositions[i] : newPositions[i];
					Project.ActiveProject.controller.Select(element, true);
				}

				for (int i = 0; i < devChip.Wires.Count; i++)
				{
					devChip.Wires[i].MoveOffset = wireMoveOffsets[i] * (undo ? -1 : 1);
					devChip.Wires[i].ApplyMoveOffset();
				}
			}
		}

		class WireExistenceAction : UndoAction
		{
			public WireDescription wireDescription;
			public int wireIndex;
			public bool isDeleteAction;
			public FullWireState fullWireStateBeforeDelete;


			public void Trigger(bool undo, DevChipInstance devChip)
			{
				if (!isDeleteAction) undo = !undo;
				bool addWire = undo;

				if (addWire)
				{
					if (fullWireStateBeforeDelete != null)
					{
						fullWireStateBeforeDelete.Restore(devChip);
					}
					else
					{
						(WireInstance loadedWire, bool failed) = DevChipInstance.TryLoadWireFromDescription(wireDescription, wireIndex, devChip, devChip.Wires);
						if (failed) throw new Exception("Failed to load wire in undo/redo action");
						else devChip.AddWire(loadedWire, false, wireIndex);
					}
				}
				else
				{
					devChip.DeleteWire(devChip.Wires[wireIndex]);
				}
			}
		}

		class FullWireState
		{
			public WireDescription[] wireDescriptions;
			public bool[] createFlags;

			public void Restore(DevChipInstance devChip)
			{
				for (int i = 0; i < wireDescriptions.Length; i++)
				{
					//Debug.Log($"Restoring wire {i} | Create new = {createFlags[i]}");
					(WireInstance loadedWire, bool failed) res = DevChipInstance.TryLoadWireFromDescription(wireDescriptions[i], i, devChip, devChip.Wires);
					if (res.failed) throw new Exception("Failed to load wire in undo/redo action");

					if (createFlags[i]) devChip.AddWire(res.loadedWire, false, i);
					else devChip.Wires[i] = res.loadedWire;
				}
			}
		}

		class ElementExistenceAction : UndoAction
		{
			public string[] chipNames;
			public SubChipDescription[] subchipDescriptions;

			public PinDescription[] pinDescriptions;
			public bool[] pinInInputFlags;
			public FullWireState wireStateBeforeDelete;

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
					else
					{
						if (!devChip.TryDeleteDevPinByID(pinDescription.ID)) throw new Exception("Failed to delete dev pin");
					}
				}

				if (addElement && wireStateBeforeDelete != null)
				{
					wireStateBeforeDelete.Restore(devChip);
				}
			}
		}

		class UndoAction
		{
		}
	}
}