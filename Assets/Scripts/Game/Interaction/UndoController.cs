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

		public void TryRedo()
		{
			if (undoIndex == undoHistory.Count - 1) return;

			UndoAction action = undoHistory[undoIndex + 1];
			undoIndex++;

			UndoRedo(action, false);
		}

		public void TryUndo()
		{
			if (undoIndex == -1) return;

			UndoAction action = undoHistory[undoIndex];
			undoIndex--;

			UndoRedo(action, true);
		}

		void UndoRedo(UndoAction action, bool undo)
		{
			Project.ActiveProject.controller.CancelEverything();

			if (action is MoveUndoAction move)
			{
				move.Trigger(undo, devChip.Elements);
			}
			else if (action is DeleteUndoAction delete)
			{
				delete.Trigger(undo, devChip);
			}
		}

		public void RecordMoveUndoAction(List<IMoveable> movedElements)
		{
			MoveUndoAction moveUndoAction = new()
			{
				subChipIDs = movedElements.Select(element => element.ID).ToArray(),
				originalPositions = movedElements.Select(element => element.MoveStartPosition).ToArray(),
				newPositions = movedElements.Select(element => element.Position).ToArray(),
			};

			RecordUndoAction(moveUndoAction);
		}

		public void RecordDeleteSubchipAction(List<IMoveable> deletedElements)
		{
			SubChipInstance[] deletedSubChips = deletedElements.OfType<SubChipInstance>().ToArray();
			DevPinInstance[] deletedDevPins = deletedElements.OfType<DevPinInstance>().ToArray();

			DeleteUndoAction deleteAction = new()
			{
				chipNames = deletedSubChips.Select(s => s.Description.Name).ToArray(),
				subchipDescriptions = deletedSubChips.Select(DescriptionCreator.CreateSubChipDescription).ToArray(),
				pinDescriptions = deletedDevPins.Select(DescriptionCreator.CreatePinDescription).ToArray(),
				pinInInputFlags = deletedDevPins.Select(p => p.IsInputPin).ToArray(),
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


		public class MoveUndoAction : UndoAction
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
				}
			}
		}

		public class DeleteUndoAction : UndoAction
		{
			public string[] chipNames;
			public SubChipDescription[] subchipDescriptions;

			public PinDescription[] pinDescriptions;
			public bool[] pinInInputFlags;

			public void Trigger(bool undo, DevChipInstance devChip)
			{
				for (int i = 0; i < chipNames.Length; i++)
				{
					ChipDescription description = Project.ActiveProject.chipLibrary.GetChipDescription(chipNames[i]);
					if (ChipTypeHelper.IsBusType(description.ChipType)) continue; // TODO

					if (undo)
					{
						SubChipInstance subchip = new(description, subchipDescriptions[i]);
						devChip.AddNewSubChip(subchip, false);
					}
					else devChip.TryDeleteSubChipByID(subchipDescriptions[i].ID);
				}

				for (int i = 0; i < pinDescriptions.Length; i++)
				{
					PinDescription pinDescription = pinDescriptions[i];

					if (undo)
					{
						DevPinInstance devPin = new(pinDescription, pinInInputFlags[i]);
						devChip.AddNewDevPin(devPin, false);
					}
					else devChip.TryDeleteDevPinByID(pinDescription.ID);
				}
			}
		}

		public class UndoAction
		{
		}
	}
}