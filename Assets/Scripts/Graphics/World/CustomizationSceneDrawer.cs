using System;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using UnityEngine;

namespace DLS.Graphics
{
	public static class CustomizationSceneDrawer
	{
		static Vector2Int selectedChipResizeDir;
		static Vector2 chipResizeMouseStartPos;
		static Vector2 chipResizeStartSize;

		static DisplayInteractState displayInteractState;
		public static DisplayInstance SelectedDisplay;
		static DisplayInstance DisplayUnderMouse;
		static Vector2 displayMoveMouseOffset;
		static Vector2 mouseDownPos;
		static Vector2 displayPosInitial;
		static float displayScaleInitial;

		static SubChipInstance CustomizeChip => ChipSaveMenu.ActiveCustomizeChip;

		public static bool IsPlacingDisplay => displayInteractState == DisplayInteractState.Placing;

		public static void DrawCustomizationScene()
		{
			SubChipInstance chip = ChipSaveMenu.ActiveCustomizeChip;

			HandleKeyboardShortcuts();

			DevSceneDrawer.DrawSubChip(chip);
			WorldDrawer.DrawGridIfActive(ColHelper.MakeCol255(0, 0, 0, 100));

			Draw.StartLayer(Vector2.zero, 1, false);
			DevSceneDrawer.DrawSubchipDisplays(chip, null, true);

			bool chipResizeHascontrol = HandleChipResizing(chip);
			HandleDisplaySelection(!chipResizeHascontrol);

			if (SelectedDisplay == null)
			{
				if (DisplayUnderMouse != null) HandleDeleteDisplayUnderMouse();
			}
			else
			{
				if (displayInteractState == DisplayInteractState.Scaling)
				{
					HandleDisplayScaling();
				}
				else
				{
					HandleDisplayMovement();
				}
			}

			// Display highlighted pin name
			if (InteractionState.ElementUnderMouse is PinInstance highlightedPin)
			{
				Draw.StartLayer(Vector2.zero, 1, false);
				DevSceneDrawer.DrawPinLabel(highlightedPin);
			}
		}

		static void HandleKeyboardShortcuts()
		{
			
		}

		public static void StartPlacingDisplay(SubChipInstance subChipToDisplay)
		{
			SelectedDisplay = new DisplayInstance();
			SelectedDisplay.Desc = new DisplayDescription(subChipToDisplay.ID, Vector2.zero, 1);
			SelectedDisplay.ChildDisplays = subChipToDisplay.Displays;

			displayInteractState = DisplayInteractState.Placing;
			displayMoveMouseOffset = Vector2.zero;
		}

		public static void OnCustomizationMenuClosed()
		{
			selectedChipResizeDir = Vector2Int.zero;
		}

		public static void OnCustomizationMenuOpened()
		{
		}

		static void HandleDisplayScaling()
		{
			Draw.StartLayer(Vector2.zero, 1, false);

			Color scaleCol = new(0.4f, 1, 0.2f);
			float deltaScale = (mouseDownPos - InputHelper.MousePosWorld).magnitude;
			deltaScale *= Vector2.Dot((InputHelper.MousePosWorld - mouseDownPos).normalized, (displayPosInitial - mouseDownPos).normalized);
			float targetScale = Mathf.Max(DrawSettings.GridSize, displayScaleInitial - deltaScale);

			if (!Project.ActiveProject.ShouldSnapToGrid)
			{
				SelectedDisplay.Desc.Scale = targetScale;
			}


			Bounds2D bounds = DevSceneDrawer.DrawDisplayWithBackground(SelectedDisplay, Vector2.zero, ChipSaveMenu.ActiveCustomizeChip);
			DrawDisplayBoundsIndicators(bounds, scaleCol);

			if (Project.ActiveProject.ShouldSnapToGrid)
			{
				float unscaledWidth = bounds.Width / SelectedDisplay.Desc.Scale;
				float scaledWidth = unscaledWidth * targetScale;

				float snappedWidth = GridHelper.SnapToGrid(scaledWidth);
				float snappedScale = snappedWidth / unscaledWidth;

				SelectedDisplay.Desc.Scale = snappedScale;
			}

			// Exit (confirm/cancel)
			bool cancel = KeyboardShortcuts.CancelShortcutTriggered || InputHelper.IsMouseDownThisFrame(MouseButton.Right);

			if (cancel)
			{
				SelectedDisplay.Desc.Position = displayPosInitial;
				SelectedDisplay.Desc.Scale = displayScaleInitial;
				CustomizeChip.Displays.Add(SelectedDisplay);
				SelectedDisplay = null;
				displayInteractState = DisplayInteractState.None;
			}
			else
			{
				bool confirm = InputHelper.IsMouseUpThisFrame(MouseButton.Left);

				if (confirm)
				{
					ChipSaveMenu.ActiveCustomizeChip.Displays.Add(SelectedDisplay);
					SelectedDisplay = null;
					displayInteractState = DisplayInteractState.None;
				}
			}
		}

		static void HandleDisplayMovement()
		{
			Draw.StartLayer(Vector2.zero, 1, false);
			Vector2 targetPos = InputHelper.MousePosWorld + displayMoveMouseOffset;

			if (!Project.ActiveProject.ShouldSnapToGrid)
			{
				SelectedDisplay.Desc.Position = targetPos;
			}

			Bounds2D bounds = DevSceneDrawer.DrawDisplayWithBackground(SelectedDisplay, Vector2.zero, ChipSaveMenu.ActiveCustomizeChip);
			DrawDisplayBoundsIndicators(bounds, Color.white);

			if (Project.ActiveProject.ShouldSnapToGrid)
			{
				Vector2 snapPointOffset = bounds.TopLeft - bounds.Centre;
				Vector2 snap = GridHelper.SnapMovingElementToGrid(targetPos, snapPointOffset, true, true);
				SelectedDisplay.Desc.Position = snap;
			}

			bool cancelMovement = KeyboardShortcuts.CancelShortcutTriggered || InputHelper.IsMouseDownThisFrame(MouseButton.Right);
			bool delete = InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete);

			if (cancelMovement || delete)
			{
				SelectedDisplay.Desc.Position = displayPosInitial;
				if (!delete && displayInteractState == DisplayInteractState.Moving) CustomizeChip.Displays.Add(SelectedDisplay);
				SelectedDisplay = null;
				displayInteractState = DisplayInteractState.None;
			}
			else
			{
				// Confirm placement
				bool confirmPlacement = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
				confirmPlacement |= displayInteractState == DisplayInteractState.Moving && InputHelper.IsMouseUpThisFrame(MouseButton.Left);

				if (confirmPlacement)
				{
					ChipSaveMenu.ActiveCustomizeChip.Displays.Add(SelectedDisplay);
					SelectedDisplay = null;
					displayInteractState = DisplayInteractState.None;
				}
			}
		}

		static void HandleDeleteDisplayUnderMouse()
		{
			bool delete = InputHelper.IsKeyDownThisFrame(KeyCode.Backspace) || InputHelper.IsKeyDownThisFrame(KeyCode.Delete);
			if (delete)
			{
				CustomizeChip.Displays.Remove(DisplayUnderMouse);
				DisplayUnderMouse = null;
			}
		}

		static void HandleDisplaySelection(bool canSelect)
		{
			DisplayUnderMouse = null;
			if (!canSelect) return;

			const float v = 0.85f;
			Color mouseOverIndicatorCol = new(v, v, v, 1);
			Color mouseOverIndicatorScaleCol = new(1, 0.8f, 0.2f);

			foreach (DisplayInstance display in CustomizeChip.Displays)
			{
				Bounds2D bounds = display.LastDrawBounds;
				float displayMinAxisSize = Mathf.Min(bounds.Width, bounds.Height);

				if (displayInteractState == DisplayInteractState.None)
				{
					if (InputHelper.MouseInsideBounds_World(bounds))
					{
						DisplayUnderMouse = display;

						float cornerDst = bounds.DstToCorner(InputHelper.MousePosWorld);
						float cornerDstThresholdForScaleMode = Mathf.Min(displayMinAxisSize * 0.2f, DrawSettings.GridSize * 1.5f);
						bool enterScaleMode = cornerDst < cornerDstThresholdForScaleMode;

						if (enterScaleMode)
						{
							DrawClosestCornerDisplayBoundsIndicator(bounds, InputHelper.MousePosWorld, mouseOverIndicatorScaleCol);
						}
						else
						{
							DrawDisplayBoundsIndicators(bounds, mouseOverIndicatorCol);
						}


						if (InputHelper.IsMouseDownThisFrame(MouseButton.Left, true))
						{
							displayInteractState = enterScaleMode ? DisplayInteractState.Scaling : DisplayInteractState.Moving;
							SelectedDisplay = display;
							CustomizeChip.Displays.Remove(display); // remove from displays while moving (so can be drawn separately on top of everything else)
							displayMoveMouseOffset = display.Desc.Position - InputHelper.MousePosWorld;
							displayPosInitial = display.Desc.Position;
							displayScaleInitial = display.Desc.Scale;
							mouseDownPos = InputHelper.MousePosWorld;
							return; // exit now that a display has been selected
						}
					}
				}
			}
		}

		static void DrawDisplayBoundsIndicators(Bounds2D bounds, Color col)
		{
			DrawPlacementCornerIndicator(bounds.TopLeft, Vector2.right, Vector2.down, col);
			DrawPlacementCornerIndicator(bounds.TopRight, Vector2.left, Vector2.down, col);
			DrawPlacementCornerIndicator(bounds.BottomLeft, Vector2.right, Vector2.up, col);
			DrawPlacementCornerIndicator(bounds.BottomRight, Vector2.left, Vector2.up, col);
		}

		static void DrawClosestCornerDisplayBoundsIndicator(Bounds2D bounds, Vector2 point, Color col)
		{
			Span<Vector2> corners = stackalloc Vector2[4]
			{
				bounds.TopLeft,
				bounds.TopRight,
				bounds.BottomLeft,
				bounds.BottomRight
			};

			int cornerIndex = 0;
			for (int i = 1; i < corners.Length; i++)
			{
				if ((corners[i] - point).sqrMagnitude < (corners[cornerIndex] - point).sqrMagnitude)
				{
					cornerIndex = i;
				}
			}

			float dirX = -((cornerIndex & 1) * 2 - 1);
			float dirY = cornerIndex <= 1 ? -1 : 1;
			DrawPlacementCornerIndicator(corners[cornerIndex], new Vector2(dirX, 0), new Vector2(0, dirY), col);
		}

		static void DrawPlacementCornerIndicator(Vector2 corner, Vector2 dirA, Vector2 dirB, Color col)
		{
			const float pad = 0.0f;
			const float len = DrawSettings.GridSize;
			const float thick = 0.01f;

			Vector2 origin = corner - (dirA + dirB) * pad;
			Draw.Line(origin, origin + dirA * len, thick, col);
			Draw.Line(origin, origin + dirB * len, thick, col);
		}

		static bool HandleChipResizing(SubChipInstance chip)
		{
			const float pad = 0.25f;
			const float h = 1.1f;
			const float size = 0.12f;
			bool canInteract = displayInteractState == DisplayInteractState.None;
			bool hascontrol = false;
			// Draw resize arrow handles on all sides of chip
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreRight, Vector2Int.right);
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreLeft, Vector2Int.left);
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreTop, Vector2Int.up);
			hascontrol |= DrawScaleHandle(chip.SelectionBoundingBox.CentreBottom, Vector2Int.down);
			return hascontrol;

			bool DrawScaleHandle(Vector2 edge, Vector2Int dir)
			{
				Vector2 dirVec = dir;
				edge += dirVec * pad;
				Vector2 perp = new(-dirVec.y, dir.x);
				Vector2 a = edge + dirVec * size;
				Vector2 b = edge - (dir + perp * h) * size;
				Vector2 c = edge - (dir - perp * h) * size;

				bool mouseOver = canInteract && Maths.TriangleContainsPoint(InputHelper.MousePosWorld, a, b, c);
				if (mouseOver && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					selectedChipResizeDir = dir;
					chipResizeMouseStartPos = InputHelper.MousePosWorld;
					chipResizeStartSize = chip.Size;
				}

				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left)) selectedChipResizeDir = Vector2Int.zero;

				bool selected = canInteract && selectedChipResizeDir == dir;
				Color col = mouseOver ? ColHelper.MakeCol(0.7f) : ColHelper.MakeCol(0.3f);
				if (selected)
				{
					col = Color.white;
					Vector2 mouseDelta = InputHelper.MousePosWorld - chipResizeMouseStartPos;
					Vector2 desiredSize = chipResizeStartSize + Vector2.Scale(dir, mouseDelta) * 2;

					// Always snap chip height so that pins align with grid lines/centers
					float deltaY = GridHelper.SnapToGrid(desiredSize.y - chip.MinSize.y);
					desiredSize.y = chip.MinSize.y + deltaY;
					// Snap chip width to grid lines if in snap mode
					if (Project.ActiveProject.ShouldSnapToGrid && dir.x != 0) desiredSize.x = GridHelper.SnapToGridForceEven(desiredSize.x) - DrawSettings.ChipOutlineWidth;

					Vector2 sizeNew = Vector2.Max(desiredSize, chip.MinSize);

					if (sizeNew != chip.Size)
					{
						chip.Description.Size = Vector2.Max(desiredSize, chip.MinSize);
						ChipSaveMenu.ActiveCustomizeChip.UpdatePinLayout();
					}
				}
				// Highlight opposite handle to selected handle
				else if (dir == -selectedChipResizeDir)
				{
					col = Color.white;
				}

				Draw.Triangle(a, b, c, col);
				return mouseOver || selected;
			}
		}

		public static void Reset()
		{
			SelectedDisplay = null;
			displayInteractState = DisplayInteractState.None;
		}

		enum DisplayInteractState
		{
			None,
			Moving,
			Placing,
			Scaling
		}
	}
}