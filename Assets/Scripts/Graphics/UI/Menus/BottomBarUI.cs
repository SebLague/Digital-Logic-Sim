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

        static bool showCommentTooltip = false;
        static string commentTooltipText = "";
        static Vector2 commentTooltipPosition = Vector2.zero;
		static StarredItem? hoveredStarredItem = null;
		static Bounds2D hoveredButtonBounds = Bounds2D.CreateEmpty();
        static bool tooltipHoverSourceIsPopup = false; 


		static bool MenuButtonsAndShortcutsEnabled => Project.ActiveProject.CanEditViewedChip;

		public static void DrawUI(Project project)
		{
            // Reset hover state at the beginning of the frame
            showCommentTooltip = false;
            commentTooltipText = "";
			hoveredStarredItem = null;
            hoveredButtonBounds = Bounds2D.CreateEmpty();
            tooltipHoverSourceIsPopup = false;


			DrawBottomBar(project); 

			if (UIDrawer.ActiveMenu == UIDrawer.MenuType.BottomBarMenuPopup)
			{
				DrawPopupMenu();
			}
            else if (showCommentTooltip && !string.IsNullOrEmpty(commentTooltipText))
            {
                DrawCommentTooltip();
            }


			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.BottomBarMenuPopup or UIDrawer.MenuType.None)
			{
				HandleKeyboardShortcuts();
			}
		}

		static void DrawPopupMenu()
		{
			ButtonTheme theme = DrawSettings.ActiveUITheme.MenuPopupButtonTheme;
			float menuWidth = Draw.CalculateTextBoundsSize(menuButtonNames[0], theme.fontSize, theme.font).x + 1;
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
			int commentPreference = project.description.Prefs_ShowChipCommentsBottomBar; // Use the correct preference

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

					bool canAdd = starred.IsCollection || project.ViewedChip.CanAddSubchip(starred.Name);

					if (UI.Button(buttonName, buttonTheme, buttonPos, buttonSize, chipButtonsEnabled && canAdd, true, false, Anchor.BottomLeft, textOffsetX: textOffsetX, ignoreInputs: ignoreInputs))
					{
						if (starred.IsCollection)
						{
							ChipCollection newActiveCollection = GetChipCollectionByName(starred.Name);
							if (newActiveCollection.Chips.Count > 0 && KeyboardShortcuts.TakeFirstFromCollectionModifierHeld)
							{
								project.controller.StartPlacing(newActiveCollection.Chips[0]);
								activeCollection = null;
							}
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

                    Bounds2D currentButtonBounds = UI.PrevBounds;
                    bool isHoveringCurrentButton = UI.MouseInsideBounds(currentButtonBounds);

					if(isHoveringCurrentButton && activeCollection == null && !ignoreInputs && !starred.IsCollection)
					{
						hoveredStarredItem = starred;
                        hoveredButtonBounds = currentButtonBounds; // Store button bounds for positioning
                        tooltipHoverSourceIsPopup = false; // Set hover source flag
						bool showBasedOnPreference = false;

						if (commentPreference == PreferencesMenu.DisplayMode_OnHover)
						{
							showBasedOnPreference = true;
						}
						else if (commentPreference == PreferencesMenu.DisplayMode_OnHover_ALT) 
						{
							showBasedOnPreference = InputHelper.AltIsHeld;
						}

						if(showBasedOnPreference)
						{
							if (project.chipLibrary.TryGetChipDescription(starred.Name, out ChipDescription chipDesc))
							{
								if (!string.IsNullOrEmpty(chipDesc.ChipComment))
								{
									showCommentTooltip = true; 
									commentTooltipText = chipDesc.ChipComment;
								}
							}
						}
					}

					if (isRightClick && UI.MouseInsideBounds(currentButtonBounds))
					{
						ContextMenu.OpenBottomBarContextMenu(starred.Name, starred.IsCollection, false);
					}


					buttonPosX += currentButtonBounds.Width + buttonSpacing;
				}

				// Record total width of all buttons to be used as scroll bounds for the next frame
				chipBarTotalWidthLastFrame = UI.PrevBounds.Right - firstButtonLeft + buttonSpacing;
			} // End Mask Scope for Main Bar


			// Draw collection popup (if active)
			if (activeCollection != null && activeCollection.Chips.Count > 0)
			{
				Vector2 bottomLeftCurr = collectionPopupBottomLeft + new Vector2(0, 0);
				Draw.ID popupPanelID = UI.ReservePanel();
				float maxWidth = 0;
				int pressedIndex = -1;
				bool openedContextMenu = false;

                const float popupOffsetX = 0.55f;
                maxWidth = 0f;
                foreach (string chipName in activeCollection.Chips)
                {
                    Vector2 textSize = Draw.CalculateTextBoundsSize(chipName, buttonTheme.fontSize, buttonTheme.font);
                    float estimatedButtonWidth = textSize.x + DrawSettings.DefaultButtonSpacing * 2 + popupOffsetX; 
                    maxWidth = Mathf.Max(maxWidth, estimatedButtonWidth);
                }
                 maxWidth = Mathf.Max(maxWidth, 5f);


                Bounds2D popupTotalBounds = Bounds2D.CreateEmpty();
				for (int i = activeCollection.Chips.Count - 1; i >= 0; i--)
				{
					const bool leftAlign = true;
					string chipName = activeCollection.Chips[i];
					bool enabled = project.ViewedChip.CanAddSubchip(chipName);

					if (UI.Button(chipName, buttonTheme, bottomLeftCurr, new Vector2(maxWidth, buttonHeight), enabled, false, false, Anchor.BottomLeft, leftAlign, popupOffsetX, ignoreInputs: ignoreInputs))
					{
						pressedIndex = i;
					}

                    Bounds2D currentPopupButtonBounds = UI.PrevBounds;
                    popupTotalBounds = Bounds2D.Grow(popupTotalBounds, currentPopupButtonBounds);
                    bool isHoveringCurrentPopupButton = UI.MouseInsideBounds(currentPopupButtonBounds);

                    if (isHoveringCurrentPopupButton && !ignoreInputs)
                    {
						hoveredStarredItem = null;
                        hoveredButtonBounds = currentPopupButtonBounds; 
                        tooltipHoverSourceIsPopup = true; 
                        bool showBasedOnPreference = false;


                        if (commentPreference == PreferencesMenu.DisplayMode_OnHover)
                        {
                            showBasedOnPreference = true;
                        }
                        else if (commentPreference == PreferencesMenu.DisplayMode_OnHover_ALT)
                        {
                            showBasedOnPreference = InputHelper.AltIsHeld;
                        }

                        if (showBasedOnPreference)
                        {
                            if (project.chipLibrary.TryGetChipDescription(chipName, out ChipDescription chipDesc))
                            {
                                if (!string.IsNullOrEmpty(chipDesc.ChipComment))
                                {
                                    showCommentTooltip = true; 
                                    commentTooltipText = chipDesc.ChipComment;
                                }
                            }
                        }
                    }

					else if (isRightClick && isHoveringCurrentPopupButton)
					{
						ContextMenu.OpenBottomBarContextMenu(chipName, false, true);
						openedContextMenu = true;
					}

					bottomLeftCurr = currentPopupButtonBounds.TopLeft + Vector2.up * buttonSpacing;
				}

                // Draw popup background panel using calculated max width and height
				UI.ModifyPanel(popupPanelID, collectionPopupBottomLeft + Vector2.left * buttonSpacing, new Vector2(maxWidth + buttonSpacing * 2, popupTotalBounds.Height + buttonSpacing * 2), theme.StarredBarCol, Anchor.BottomLeft);


				if (!openedContextMenu)
				{
					if (pressedIndex != -1)
					{
						project.controller.StartPlacing(project.chipLibrary.GetChipDescription(activeCollection.Chips[pressedIndex]));
						if (KeyboardShortcuts.MultiModeHeld) closeActiveCollectionMultiModeExit = true;
						else activeCollection = null;
					}
                    else if (InputHelper.IsAnyMouseButtonDownThisFrame_IgnoreConsumed() && Time.frameCount != collectionInteractFrame && !UI.MouseInsideBounds(popupTotalBounds))
                    {
                         activeCollection = null;
                    }
                    else if (KeyboardShortcuts.CancelShortcutTriggered || UIDrawer.ActiveMenu != UIDrawer.MenuType.None)
					{
						activeCollection = null;
					}
				}
			}
		}

        static void DrawCommentTooltip()
        {
            if (hoveredButtonBounds.Size == Vector2.zero) return; 

            DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
            FontType font = FontType.JetbrainsMonoRegular;
            float fontSize = theme.FontSizeRegular * 0.9f;
            Color textColor = Color.black;
            Color bgColor = Color.white * 0.95f;
            float maxWidth = 30f;
            float padding = 0.5f;
            float sideOffset = 0.3f;

            string wrappedComment = ChipLibraryMenu.WrapText(commentTooltipText, font, fontSize, maxWidth - padding * 2);

            Vector2 textSize = Draw.CalculateTextBoundsSize(wrappedComment, fontSize, font);
            Vector2 panelSize = textSize + Vector2.one * padding * 2;


            if (tooltipHoverSourceIsPopup) 
            {
                Vector2 mouseUIPos = UI.ScreenToUISpace(InputHelper.MousePos);
                bool preferRight = mouseUIPos.x > hoveredButtonBounds.Centre.x;

                Vector2 positionRight = hoveredButtonBounds.CentreRight + Vector2.right * (panelSize.x / 2f + sideOffset);
                Vector2 positionLeft = hoveredButtonBounds.CentreLeft + Vector2.left * (panelSize.x / 2f + sideOffset);

                bool cantFitRight = positionRight.x + panelSize.x / 2f > UI.Width;
                bool cantFitLeft = positionLeft.x - panelSize.x / 2f < 0;

                if (preferRight && !cantFitRight) {
                    commentTooltipPosition = positionRight;
                } else if (!preferRight && !cantFitLeft) {
                    commentTooltipPosition = positionLeft;
                } else if (!cantFitRight) { 
                    commentTooltipPosition = positionRight;
                } else if (!cantFitLeft) { 
                    commentTooltipPosition = positionLeft;
                } else {
                    commentTooltipPosition = hoveredButtonBounds.CentreTop + Vector2.up * (panelSize.y / 2f + sideOffset);
                }
                 commentTooltipPosition.y = hoveredButtonBounds.Centre.y;

            }
            else
            {
                 commentTooltipPosition = hoveredButtonBounds.CentreTop + Vector2.up * (panelSize.y / 2f + sideOffset);
            }


            commentTooltipPosition.y = Mathf.Clamp(commentTooltipPosition.y, panelSize.y / 2f + barHeight, UI.Height - panelSize.y / 2f);
            commentTooltipPosition.x = Mathf.Clamp(commentTooltipPosition.x, panelSize.x / 2f, UI.Width - panelSize.x / 2f);


			Draw.StartLayer(Vector2.zero, 1, true);
            UI.DrawPanel(commentTooltipPosition, panelSize, bgColor);
            UI.DrawText(wrappedComment, font, fontSize, commentTooltipPosition, Anchor.TextCentre, textColor);
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

			throw new Exception($"Failed to find collection with name: {name}"); 
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
            showCommentTooltip = false;
            commentTooltipText = "";
			hoveredStarredItem = null;
            hoveredButtonBounds = Bounds2D.CreateEmpty();
            tooltipHoverSourceIsPopup = false; 
		}
	}
}