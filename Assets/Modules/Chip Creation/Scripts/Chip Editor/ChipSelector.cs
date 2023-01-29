using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	// Handles selection of placed chips.
	// Chips can be selected by clicking on them, or by clicking and dragging over multiple chips.
	public class ChipSelector : ControllerBase
	{
		public event System.Action<ChipBase> ChipSelected;
		public ReadOnlyCollection<ChipBase> SelectedChips => new(selectedChips);
		public int NumSelectedChips => selectedChips.Count;
		public bool IsBoxSelecting => isBoxSelecting;
		public override bool IsBusy() => IsBoxSelecting;

		[SerializeField] Transform selectionBox;

		List<ChipBase> selectedChips;
		Vector2 selectionStartPos;
		bool isBoxSelecting;
		bool chipSelectedThisFrame;

		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);


			editor.SubChipAdded += OnSubChipAdded;
			editor.ChipPlacer.StartedPlacingOrLoadingChip += OnChipCreated;
			editor.WorkArea.WorkAreaMouseInteraction.LeftMouseDown += LeftMouseDownInWorkArea;

			selectedChips = new List<ChipBase>();
			selectionBox.gameObject.SetActive(false);

		}

		void LateUpdate()
		{
			HandleInput();
			DrawBoxSelection();
		}

		void HandleInput()
		{
			Mouse mouse = Mouse.current;
			Keyboard keyboard = Keyboard.current;

			// A catch-all for deselecting chips when clicking anywhere (other than on a chip)
			if (mouse.leftButton.wasPressedThisFrame && !chipSelectedThisFrame && !chipEditor.ChipPlacer.IsBusy())
			{
				DeselectAll();
			}

			if (isBoxSelecting)
			{
				if (mouse.leftButton.wasReleasedThisFrame)
				{
					FinishBoxSelection();
				}
				else if (mouse.rightButton.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame || chipEditor.WireEditor.IsCreatingWire)
				{
					CancelBoxSelection();
				}
			}

			chipSelectedThisFrame = false;
		}

		void DrawBoxSelection()
		{
			if (isBoxSelecting)
			{
				Vector2 selectionBoxEndPos = MouseHelper.GetMouseWorldPosition();
				Vector2 selectionBoxSize = selectionStartPos - selectionBoxEndPos;
				Vector2 centre = (selectionStartPos + selectionBoxEndPos) / 2;
				selectionBox.position = new Vector3(centre.x, centre.y, RenderOrder.ChipMoving);
				selectionBox.localScale = new Vector3(Mathf.Abs(selectionBoxSize.x), Mathf.Abs(selectionBoxSize.y), 1);
			}
		}

		void LeftMouseDownInWorkArea(WorkArea workArea)
		{
			if (!chipEditor.ChipPlacer.IsBusy())
			{
				DeselectAll();
				StartBoxSelection();
			}

		}

		void StartBoxSelection()
		{
			isBoxSelecting = true;
			selectionStartPos = MouseHelper.GetMouseWorldPosition();
			selectionBox.gameObject.SetActive(true);
		}

		void FinishBoxSelection()
		{
			Vector2 selectionBoxSize = selectionBox.localScale;

			if (selectionBoxSize.magnitude > 0.01f)
			{
				Vector2 selectionBoxMin = (Vector2)selectionBox.transform.position - selectionBoxSize / 2;
				Vector2 selectionBoxMax = (Vector2)selectionBox.transform.position + selectionBoxSize / 2;

				foreach (ChipBase chip in chipEditor.AllSubChips)
				{
					if (chip is BusDisplay)
					{
						continue;
					}
					Vector2 chipBoundsMin = (Vector2)chip.transform.position - chip.Size / 2;
					Vector2 chipBoundsMax = (Vector2)chip.transform.position + chip.Size / 2;

					if (BoundsOverlap2D(selectionBoxMin, selectionBoxMax, chipBoundsMin, chipBoundsMax))
					{
						AddToSelection(chip);
					}

				}
			}
			CancelBoxSelection();

			bool BoundsOverlap2D(Vector2 minA, Vector2 maxA, Vector2 minB, Vector2 maxB)
			{
				return minA.x < maxB.x && minB.x < maxA.x && minA.y < maxB.y && minB.y < maxA.y;
			}
		}

		void CancelBoxSelection()
		{
			selectionBox.gameObject.SetActive(false);
			selectionBox.localScale = Vector3.zero;
			isBoxSelecting = false;
		}

		public void DeselectAll()
		{
			//Debug.Log("Deselect");
			foreach (var chip in selectedChips)
			{
				chip.SetHighlightState(false);
			}
			selectedChips.Clear();
		}

		void OnSubChipAdded(ChipBase chip)
		{
			chip.ChipDeleted -= OnChipDeleted;
			chip.ChipDeleted += OnChipDeleted;
			if (chip.MouseInteraction is not null)
			{
				chip.MouseInteraction.LeftMouseDown += OnChipPressedLeftMouse;
				chip.MouseInteraction.RightMouseDown += OnChipPressedRightMouse;
			}
			Deselect(chip);

		}

		void OnChipCreated(ChipBase chip, bool loadingFromFile)
		{
			if (!loadingFromFile)
			{
				DeselectAll();
				AddToSelection(chipEditor.ChipPlacer.AllChipsInPlacementMode);
			}
			chip.ChipDeleted += OnChipDeleted;
		}


		void OnChipDeleted(ChipBase chip)
		{
			Deselect(chip);
		}

		// If chip pressed with left mouse, select only that chip.
		// But if the chip is already selected, then don't deselect other chips in case user wants to move them as a group
		void OnChipPressedLeftMouse(ChipBase chip)
		{
			if (!IsSelected(chip))
			{
				DeselectAll();
				AddToSelection(chip);
			}
			chipSelectedThisFrame = true;
		}

		// If chip pressed with right mouse, select only that chip
		void OnChipPressedRightMouse(ChipBase chip)
		{
			DeselectAll();
			AddToSelection(chip);
			chipSelectedThisFrame = true;
		}

		void AddToSelection(ChipBase[] chips)
		{
			foreach (ChipBase chip in chips)
			{
				AddToSelection(chip);
			}
		}


		void AddToSelection(ChipBase chip)
		{
			chipSelectedThisFrame = true;
			chip.SetHighlightState(true);
			if (!selectedChips.Contains(chip))
			{
				selectedChips.Add(chip);
				ChipSelected?.Invoke(chip);
			}
		}

		void Deselect(ChipBase chip)
		{
			chip.SetHighlightState(false);
			if (selectedChips.Contains(chip))
			{
				selectedChips.Remove(chip);
			}
		}

		bool IsSelected(ChipBase chip)
		{
			return selectedChips.Contains(chip);
		}

	}
}