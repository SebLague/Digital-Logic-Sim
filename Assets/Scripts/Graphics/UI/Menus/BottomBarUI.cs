using System;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class BottomBarUI
	{
		public const float barHeight = 3;
		const float padY = 0.3f;
		const float buttonSpacing = 0.25f;
		const float buttonHeight = barHeight - padY * 2;

		const int NewChipButtonIndex = 0;
		const int SaveChipButtonIndex = 1;
		const int FindChipButtonIndex = 2;
		const int LibraryButtonIndex = 3;
		const int OptionsButtonIndex = 4;
		const int QuitButtonIndex = 5;

		const string c = "<color=#666666ff>";

		static readonly string[] menuButtonNames =
		{
			$"NEW CHIP     {c}Ctrl+N",
			$"SAVE CHIP    {c}Ctrl+S",
			$"FIND CHIP    {c}Ctrl+F",
			$"LIBRARY      {c}Ctrl+L",
			$"PREFS        {c}Ctrl+P",
			$"QUIT         {c}Ctrl+Q"
		};

		// ---- State ----
		static float scrollX;
		static float chipBarTotalWidthLastFrame;
		static bool isDraggingChipBar;
		static float mouseDragPrev;
		static bool closeActiveCollectionMultiModeExit;

		static int toggleMenuFrame;
		static int collectionInteractFrame;
		static ChipCollection activeCollection;
		static Vector2 collectionPopupBottomLeft;
		static Bounds2D barBounds_ScreenSpace;

		static bool MenuButtonsAndShortcutsEnabled => Project.ActiveProject.CanEditViewedChip;

		public static void DrawUI(Project project)
		{
			DrawBottomBar(project);

			if (UIDrawer.ActiveMenu == UIDrawer.MenuType.BottomBarMenuPopup)
			{
				DrawPopupMenu();
			}

			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.None)
			{
				HandleKeyboardShortcuts();
			}
		}

		static void DrawPopupMenu()
		{
			ButtonTheme theme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			float menuWidth = Draw.CalculateTextBoundsSize(menuButtonNames[0].AsSpan(), theme.fontSize, theme.font).x + 1;
			Vector2 pos = new(buttonSpacing, barHeight + buttonSpacing);
			Vector2 size = new(menuWidth, buttonHeight);
			Draw.ID panelID = UI.ReservePanel();

			using (UI.BeginBoundsScope(true))
			{
				for (int i = menuButtonNames.Length - 1; i >= 0; i--)
				{
					bool buttonEnabled = MenuButtonsAndShortcutsEnabled || i is QuitButtonIndex or OptionsButtonIndex;
					string text = menuButtonNames[i];
					if (UI.Button(text, theme, pos, size, buttonEnabled, false, false, Anchor.BottomLeft))
					{
						ButtonPressed(i);
					}

					pos = UI.PrevBounds.TopLeft;
				}

				Bounds2D uiBounds = UI.GetCurrentBoundsScope();
				UI.ModifyPanel(panelID, uiBounds.Centre, uiBounds.Size + Vector2.one * (buttonSpacing * 2), Color.white);
			}

			// Close if clicked nothing or pressed esc
			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup)
			{
				if (InputHelper.IsAnyMouseButtonDownThisFrame_IgnoreConsumed() && Time.frameCount != toggleMenuFrame)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}

				if (KeyboardShortcuts.CancelShortcutTriggered)
				{
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}

			void ButtonPressed(int i)
			{
				if (i == NewChipButtonIndex) CreateNewChip();
				else if (i == SaveChipButtonIndex) OpenSaveMenu();
				else if (i == FindChipButtonIndex) OpenSearchMenu();
				else if (i == LibraryButtonIndex) OpenLibraryMenu();
				else if (i == OptionsButtonIndex) OpenPreferencesMenu();
				else if (i == QuitButtonIndex) ExitToMainMenu();
			}
		}

		static void DrawBottomBar(Project project)
		{
			Bounds2D bounds_UISpace = new(Vector2.zero, new Vector2(UI.Width, barHeight));
			barBounds_ScreenSpace = UI.UIToScreenSpace(bounds_UISpace);

			bool inOtherMenu = !(UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.None);
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			// If mouse is over context menu, then ignore inputs (don't want to actually disable the buttons because the grey-out effect is distracting here)
			// Also if middle mouse is held, as may be dragging the bar so don't want to select buttons
			bool ignoreInputs = ContextMenu.HasFocus() || InputHelper.IsMouseHeld(MouseButton.Middle);
			bool isRightClick = InputHelper.IsMouseDownThisFrame(MouseButton.Right);
			if (closeActiveCollectionMultiModeExit && !KeyboardShortcuts.MultiModeHeld)
			{
				closeActiveCollectionMultiModeExit = false;
				activeCollection = null;
			}

			UI.DrawPanel(bounds_UISpace, theme.StarredBarCol);

			// Menu toggle button
			Vector2 menuButtonPos = new(buttonSpacing, padY);
			Vector2 menuButtonSize = new(1.5f, barHeight - padY * 2);
			bool menuButtonEnabled = !inOtherMenu;

			if (UI.Button("MENU", theme.MenuButtonTheme, menuButtonPos, menuButtonSize, menuButtonEnabled, true, false, Anchor.BottomLeft, ignoreInputs: ignoreInputs))
			{
				UIDrawer.ToggleBottomPopupMenu();
				toggleMenuFrame = Time.frameCount;
			}


			// Chips
			ButtonTheme buttonTheme = theme.ChipButton;

			using (UI.CreateMaskScopeMinMax(new Vector2(UI.PrevBounds.Right + buttonSpacing, 0), new Vector2(UI.Width, barHeight)))
			{
				bool chipButtonsEnabled = !inOtherMenu && project.CanEditViewedChip;

				// -- Chip bar drag/scroll input --
				if (MouseIsOverBar())
				{
					const float scrollSensitivity = 2;
					scrollX += Maths.AbsoluteMax(InputHelper.MouseScrollDelta.x, InputHelper.MouseScrollDelta.y) * -scrollSensitivity;
					if (InputHelper.IsMouseDownThisFrame(MouseButton.Middle))
					{
						isDraggingChipBar = true;
						mouseDragPrev = UI.ScreenToUISpace(InputHelper.MousePos).x;
					}
				}

				if (isDraggingChipBar)
				{
					float mouseDragNew = UI.ScreenToUISpace(InputHelper.MousePos).x;
					scrollX += mouseDragNew - mouseDragPrev;
					mouseDragPrev = mouseDragNew;
					if (InputHelper.IsMouseUpThisFrame(MouseButton.Middle))
					{
						isDraggingChipBar = false;
					}
				}

				// -- Draw --
				float chipButtonsRegionStartX = UI.PrevBounds.Right + buttonSpacing;
				float chipButtonRegionWidth = UI.Width - chipButtonsRegionStartX;

				scrollX = Mathf.Clamp(scrollX, Mathf.Min(0, chipButtonRegionWidth - chipBarTotalWidthLastFrame), 0);
				float buttonPosX = chipButtonsRegionStartX + scrollX;
				float firstButtonLeft = buttonPosX;

				for (int i = 0; i < project.description.StarredList.Count; i++)
				{
					StarredItem starred = project.description.StarredList[i];
					bool isToggledOpenCollection = activeCollection != null && ChipDescription.NameMatch(starred.Name, activeCollection.Name);
					string buttonName = starred.GetDisplayStringForBottomBar(isToggledOpenCollection);

					float textOffsetX = 0;

					Vector2 buttonPos = new(buttonPosX, padY);
					Vector2 buttonSize = new(0.5f, buttonHeight);

					if (starred.IsCollection)
					{
						textOffsetX = -0.2f;
						buttonSize.x += -0.5f;
					}

					bool canAdd = starred.IsCollection || project.ViewedChip.CanAddSubchip(buttonName);

					if (UI.Button(buttonName, buttonTheme, buttonPos, buttonSize, chipButtonsEnabled && canAdd, true, false, Anchor.BottomLeft, textOffsetX: textOffsetX, ignoreInputs: ignoreInputs))
					{
						if (starred.IsCollection)
						{
							ChipCollection newActiveCollection = GetChipCollectionByName(starred.Name);
							// Take first item from collection without opening
							if (newActiveCollection.Chips.Count > 0 && KeyboardShortcuts.TakeFirstFromCollectionModifierHeld)
							{
								project.controller.StartPlacing(newActiveCollection.Chips[0]);
								activeCollection = null;
							}
							// Open collection in popup
							else
							{
								collectionPopupBottomLeft = new Vector2(UI.PrevBounds.Left, barHeight);
								activeCollection = newActiveCollection == activeCollection ? null : newActiveCollection;
								collectionInteractFrame = Time.frameCount;
								closeActiveCollectionMultiModeExit = false;
							}
						}
						else
						{
							project.controller.StartPlacing(project.chipLibrary.GetChipDescription(starred.Name));
							activeCollection = null;
						}
					}
					else if (isRightClick && UI.MouseInsideBounds(UI.PrevBounds))
					{
						ContextMenu.OpenBottomBarContextMenu(starred.Name, starred.IsCollection, false);
					}


					buttonPosX += UI.PrevBounds.Width + buttonSpacing;
				}

				// Record total width of all buttons to be used as scroll bounds for the next frame
				chipBarTotalWidthLastFrame = UI.PrevBounds.Right - firstButtonLeft + buttonSpacing;
			}


			// Draw collection popup
			if (activeCollection != null && activeCollection.Chips.Count > 0)
			{
				Vector2 bottomLeftCurr = collectionPopupBottomLeft + new Vector2(0, 0);
				Draw.ID popupPanelID = UI.ReservePanel();
				float maxWidth = 0;
				int pressedIndex = -1;
				bool openedContextMenu = false;

				// Get max width so can draw all popup-buttons same width
				using (UI.BeginBoundsScope(false))
				{
					foreach (string chipName in activeCollection.Chips)
					{
						UI.Button(chipName, buttonTheme, bottomLeftCurr, true, Anchor.BottomLeft);
					}

					maxWidth = UI.GetCurrentBoundsScope().Width;
				}

				// Draw pop-up buttons
				for (int i = activeCollection.Chips.Count - 1; i >= 0; i--)
				{
					const bool leftAlign = true;
					const float offsetX = leftAlign ? 0.55f : 0;
					string chipName = activeCollection.Chips[i];
					bool enabled = project.ViewedChip.CanAddSubchip(chipName);
					if (UI.Button(chipName, buttonTheme, bottomLeftCurr, new Vector2(maxWidth, buttonHeight), enabled, false, false, Anchor.BottomLeft, leftAlign, offsetX))
					{
						pressedIndex = i;
					}
					else if (isRightClick && UI.MouseInsideBounds(UI.PrevBounds))
					{
						ContextMenu.OpenBottomBarContextMenu(chipName, false, true);
						openedContextMenu = true;
					}

					bottomLeftCurr = UI.PrevBounds.TopLeft + Vector2.up * buttonSpacing;
				}

				UI.ModifyPanel(popupPanelID, collectionPopupBottomLeft + Vector2.left * buttonSpacing, new Vector2(maxWidth + buttonSpacing * 2, UI.PrevBounds.Top - collectionPopupBottomLeft.y + buttonSpacing), theme.StarredBarCol, Anchor.BottomLeft);

				if (!openedContextMenu)
				{
					if (pressedIndex != -1)
					{
						project.controller.StartPlacing(project.chipLibrary.GetChipDescription(activeCollection.Chips[pressedIndex]));
						if (KeyboardShortcuts.MultiModeHeld)
						{
							closeActiveCollectionMultiModeExit = true;
						}
						else
						{
							activeCollection = null;
						}
					}
					else if (KeyboardShortcuts.CancelShortcutTriggered || (InputHelper.IsAnyMouseButtonDownThisFrame_IgnoreConsumed() && Time.frameCount != collectionInteractFrame) || UIDrawer.ActiveMenu != UIDrawer.MenuType.None)
					{
						activeCollection = null;
					}
				}
			}
		}

		static ChipCollection GetChipCollectionByName(string name)
		{
			foreach (ChipCollection c in Project.ActiveProject.description.ChipCollections)
			{
				if (ChipDescription.NameMatch(c.Name, name))
				{
					return c;
				}
			}

			throw new Exception("Failed to find collection with name: " + name);
		}

		static bool MouseIsOverBar() => InputHelper.MouseInBounds_ScreenSpace(barBounds_ScreenSpace);

		static void ExitToMainMenu()
		{
			if (Project.ActiveProject.ActiveChipHasUnsavedChanges()) UnsavedChangesPopup.OpenPopup(ExitIfTrue);
			else ExitIfTrue(true);

			static void ExitIfTrue(bool exit)
			{
				if (exit)
				{
					Project.ActiveProject.NotifyExit();
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.MainMenu);
				}
			}
		}


		static Color MakeCol(int v) => new(v / 255f, v / 255f, v / 255f, 1);
		static Color MakeCol(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f, 1);

		static void OpenSaveMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
		static void OpenSearchMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.Search);
		static void OpenLibraryMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipLibrary);
		static void OpenPreferencesMenu() => UIDrawer.SetActiveMenu(UIDrawer.MenuType.Options);

		static void CreateNewChip()
		{
			if (Project.ActiveProject.ActiveChipHasUnsavedChanges()) UnsavedChangesPopup.OpenPopup(ConfirmNewChip);
			else ConfirmNewChip(true);

			static void ConfirmNewChip(bool confirm)
			{
				if (confirm)
				{
					Project.ActiveProject.CreateBlankDevChip();
				}
			}
		}

		static void HandleKeyboardShortcuts()
		{
			if (MenuButtonsAndShortcutsEnabled)
			{
				if (KeyboardShortcuts.CreateNewChipShortcutTriggered) CreateNewChip();
				if (KeyboardShortcuts.SaveShortcutTriggered) OpenSaveMenu();
				if (KeyboardShortcuts.LibraryShortcutTriggered) OpenLibraryMenu();
			}

			if (KeyboardShortcuts.PreferencesShortcutTriggered) OpenPreferencesMenu();
			if (KeyboardShortcuts.QuitToMainMenuShortcutTriggered) ExitToMainMenu();
		}

		public static void Reset()
		{
			scrollX = 0;
			chipBarTotalWidthLastFrame = 0;
			isDraggingChipBar = false;
			activeCollection = null;
		}
	}
}