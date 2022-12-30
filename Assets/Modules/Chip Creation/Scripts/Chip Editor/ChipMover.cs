using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	public class ChipMover : ControllerBase
	{
		public event System.Action ChipsMoved;

		public override bool IsBusy() => isMovingChips;

		bool readyToStartMovingChips;
		bool isMovingChips;

		ChipBase[] chipsToMove;
		Vector2[] chipStartPositions;
		Vector2 mouseDragStartPos;

		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);

			editor.SubChipAdded += OnSubChipAdded;
			chipsToMove = null;
		}

		void LateUpdate()
		{
			HandleInput();
		}

		void HandleInput()
		{
			Mouse mouse = Mouse.current;
			Vector2 mouseDelta = MouseHelper.GetMouseWorldPosition() - mouseDragStartPos;
			// Update chip positions if mouse has moved
			if (mouseDelta.magnitude > 0.0001f)
			{
				// Initialize chips to move based on selection once mouse starts moving
				if (readyToStartMovingChips)
				{
					readyToStartMovingChips = false;
					isMovingChips = true;
					InitChipsToMove();
				}

				if (isMovingChips)
				{
					for (int i = 0; i < chipsToMove.Length; i++)
					{
						Vector2 targetPos = chipStartPositions[i] + mouseDelta;
						chipsToMove[i].transform.position = new Vector3(targetPos.x, targetPos.y, RenderOrder.ChipMoving);
					}
					OnChipsMoved(chipsToMove);
				}
			}

			// Try place chip/s when left mouse button released
			if (mouse.leftButton.wasReleasedThisFrame)
			{
				readyToStartMovingChips = false;
				if (isMovingChips)
				{
					if (IsValidPositionForMovedChips())
					{
						StopMoving();
					}
					else
					{
						CancelMove();
					}
				}
			}

			// Cancel move
			if (mouse.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				CancelMove();
			}
		}

		void OnChipPressed(ChipBase chip)
		{
			if (chipEditor.CanEdit)
			{
				readyToStartMovingChips = true;
				mouseDragStartPos = MouseHelper.GetMouseWorldPosition();
			}
		}

		void OnSubChipAdded(ChipBase chip)
		{
			if (chip.MouseInteraction is not null)
			{
				chip.MouseInteraction.LeftMouseDown += OnChipPressed;
			}
		}

		void CancelMove()
		{
			if (isMovingChips)
			{
				if (chipsToMove is not null)
				{
					for (int i = 0; i < chipsToMove.Length; i++)
					{
						chipsToMove[i].transform.position = chipStartPositions[i];
					}
				}
				OnChipsMoved(chipsToMove);
				StopMoving();
			}
		}

		void StopMoving()
		{
			if (isMovingChips)
			{
				if (chipsToMove is not null)
				{
					foreach (ChipBase chip in chipsToMove)
					{
						chip.ChipDeleted -= ChipDeletedWhileMoving;
						Vector2 currentPos = chip.transform.position;
						chip.transform.position = new Vector3(currentPos.x, currentPos.y, RenderOrder.Chip);
					}
				}
				isMovingChips = false;
				chipsToMove = null;
				chipStartPositions = null;
			}
		}

		void ChipDeletedWhileMoving(ChipBase chip)
		{
			StopMoving();
		}

		bool IsValidPositionForMovedChips()
		{
			if (chipsToMove is null)
			{
				return false;
			}
			return chipsToMove.All(chip => chipEditor.ChipPlacer.IsValidPlacement(chip));
		}

		void InitChipsToMove()
		{
			chipsToMove = chipEditor.ChipSelector.SelectedChips.ToArray();
			if (chipsToMove.Length > 0)
			{
				chipsToMove[0].ChipDeleted += ChipDeletedWhileMoving;
				chipStartPositions = chipsToMove.Select(chip => (Vector2)chip.transform.position).ToArray();
			}
			isMovingChips = chipsToMove.Length > 0;

		}

		void OnChipsMoved(ChipBase[] chips)
		{
			foreach (var chip in chips)
			{
				chip.NotifyMoved();
			}
			ChipsMoved?.Invoke();
		}

	}

}