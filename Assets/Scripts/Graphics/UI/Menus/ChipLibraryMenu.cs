using System.Collections.Generic;
using System.Linq;
using System.Text; 
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.Text.Rendering;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;


namespace DLS.Graphics
{
	public static class ChipLibraryMenu
	{
		const string defaultOtherChipsCollectionName = "OTHER";

		const int deleteMessageMaxCharsPerLine = 25;
		static readonly UIHandle ID_CollectionsScrollbar = new("ChipLibrary_CollectionsScrollbar");
		static readonly UIHandle ID_StarredScrollbar = new("ChipLibrary_StarredScrollbar");
		static readonly UIHandle ID_NameInput = new("ChipLibrary_NameField");
		static readonly UIHandle ID_ChipCommentScrollbar = new("ChipLibrary_ChipCommentScrollbar"); 
		static readonly string[] buttonNames_moveSingleStep = { "MOVE UP", "MOVE DOWN" };
		static readonly string[] buttonNames_jump = { "JUMP UP", "JUMP DOWN" };
		static readonly string[] buttonNames_chipAction = { "USE", "OPEN", "DELETE" };
		static readonly string[] buttonNames_collectionRenameOrDelete = { "RENAME", "DELETE" };

		static readonly string[][] buttonName_starUnstar =
		{
			new[] { "ADD TO STARRED" },
			new[] { "REMOVE FROM STARRED" }
		};

		static readonly bool[] interactableStates_renameDelete = { true, true };
		static readonly bool[] interactableStates_move = { true, true };
		static readonly bool[] interactableStates_starredList = { true, true, true };
		static readonly bool[] interactable_chipActionButtons = { true, true, true };

		static readonly UI.ScrollViewDrawElementFunc drawCollectionEntry = DrawCollectionEntry;
		static readonly UI.ScrollViewDrawElementFunc drawStarredEntry = DrawStarredEntry;
		static readonly UI.ScrollViewDrawContentFunc drawChipCommentContent = DrawChipCommentContent;

		// State
		static int selectedCollectionIndex;
		static int selectedChipInCollectionIndex;
		static int selectedStarredItemIndex;

		static bool creatingNewCollection;
		static bool renamingCollection;
		static bool isConfirmingChipDeletion;
		static bool isConfirmingCollectionDeletion;

		static string deleteConfirmMessage;
		static Color deleteConfirmMessageCol;
		static bool isScrolling;
		static string chipToOpenName;
		static bool wasOpenedThisFrame;

		static string currentChipCommentToDraw;
        static string currentChipName; 
		static float commentTextHeight; 

		static readonly Color deleteColWarningHigh = new(0.95f, 0.35f, 0.35f);
		static readonly Color deleteColWarningMedium = new(1f, 0.75f, 0.2f);

		// if chip is moved to another collection, it will be auto-opened. Keep track so it can be auto-closed if chip is then moved out of that collection ('just passing through')
		static ChipCollection lastAutoOpenedCollection;

		static List<ChipCollection> collections => project.description.ChipCollections;

		static Project project => Project.ActiveProject;

        static readonly StringBuilder wrapStringBuilder = new StringBuilder(256);


		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Vector2 panelEdgePadding = new(3.25f, 2.6f);

			const float interPanelSpacing = 1.5f;
			const float menuOffsetY = 1.13f;
            const float starredPanelWidthT = 0.33f; 
            const float collectionPanelWidthT = 0.33f;
            const float selectedPanelWidthT = 1 - (starredPanelWidthT + collectionPanelWidthT); 


			float panelWidthSum = UI.Width - interPanelSpacing * 2 - panelEdgePadding.x * 2;
			float panelHeight = UI.Height - panelEdgePadding.y * 2;

			Vector2 panelATopLeft = UI.TopLeft + new Vector2(panelEdgePadding.x, -panelEdgePadding.y + menuOffsetY);
			Vector2 panelSizeA = new(panelWidthSum * starredPanelWidthT, panelHeight);
			Vector2 panelBTopLeft = panelATopLeft + Vector2.right * (panelSizeA.x + interPanelSpacing);
			Vector2 panelSizeB = new(panelWidthSum * collectionPanelWidthT, panelHeight);
			Vector2 panelCTopLeft = panelBTopLeft + Vector2.right * (panelSizeB.x + interPanelSpacing);
			Vector2 panelSizeC = new(panelWidthSum * selectedPanelWidthT, panelHeight);

			isScrolling = UI.GetScrollbarState(ID_CollectionsScrollbar).isDragging
						|| UI.GetScrollbarState(ID_StarredScrollbar).isDragging
						|| UI.GetScrollbarState(ID_ChipCommentScrollbar).isDragging; 

			bool popupHasFocus = creatingNewCollection || renamingCollection || isConfirmingChipDeletion || isConfirmingCollectionDeletion;

			using (UI.BeginDisabledScope(popupHasFocus))
			{
				DrawStarredPanel(panelATopLeft, panelSizeA);
				DrawCollectionsPanel(panelBTopLeft, panelSizeB);
				DrawSelectedItemPanel(panelCTopLeft, panelSizeC);
			}

			if (KeyboardShortcuts.CancelShortcutTriggered || (KeyboardShortcuts.LibraryShortcutTriggered && !wasOpenedThisFrame))
			{
				if (popupHasFocus) ResetPopupState();
				else ExitLibrary();
			}

			wasOpenedThisFrame = false;
		}

		static void ResetPopupState()
		{
			creatingNewCollection = false;
			renamingCollection = false;
			isConfirmingChipDeletion = false;
			isConfirmingCollectionDeletion = false;

			deleteConfirmMessage = string.Empty;
		}

		static void DrawPanelHeader(string text, Vector2 topLeft, float width)
		{
			Color textCol = ColHelper.MakeCol("#3CD168");
			Color bgCol = ColHelper.MakeCol("#1D1D1D");
			MenuHelper.DrawLeftAlignTextWithBackground(text, topLeft, new Vector2(width, 2.3f), Anchor.TopLeft, textCol, bgCol, true);
		}

		static void DrawStarredPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("STARRED", topLeft, size.x);

			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);

			UI.DrawScrollView(ID_StarredScrollbar, panelContentBounds.TopLeft, panelContentBounds.Size, UILayoutHelper.DefaultSpacing, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawStarredEntry, project.description.StarredList.Count);
			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
		}

		static void DrawStarredEntry(Vector2 topLeft, float width, int index, bool isLayoutPass)
		{
			StarredItem starredItem = project.description.StarredList[index];
			ButtonTheme theme = GetButtonTheme(starredItem.IsCollection, index == selectedStarredItemIndex);

			bool entryPressed = UI.Button(starredItem.GetDisplayStringForBottomBar(false), theme, topLeft, new Vector2(width, 2), true, false, false, Anchor.TopLeft, true, 1, isScrolling);
			if (entryPressed)
			{
				selectedStarredItemIndex = index;
				selectedCollectionIndex = -1;
				selectedChipInCollectionIndex = -1;
			}
		}


		static void DrawCollectionsPanel(Vector2 topLeft, Vector2 size)
		{
			Draw.ID panelID = UI.ReservePanel();
			Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
			DrawPanelHeader("COLLECTIONS", topLeft, size.x);
			Bounds2D panelBoundsMinusHeader = Bounds2D.CreateFromTopLeftAndSize(UI.PrevBounds.BottomLeft, new Vector2(size.x, size.y - UI.PrevBounds.Height));
			Bounds2D panelContentBounds = Bounds2D.Shrink(panelBoundsMinusHeader, PanelUIPadding);

			UI.DrawScrollView(ID_CollectionsScrollbar, panelContentBounds.TopLeft, panelContentBounds.Size, UILayoutHelper.DefaultSpacing, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawCollectionEntry, collections.Count);
			MenuHelper.DrawReservedMenuPanel(panelID, panelBounds, false);
		}

		static void DrawCollectionEntry(Vector2 topLeft, float width, int collectionIndex, bool isLayoutPass)
		{
			ChipCollection collection = collections[collectionIndex];
			string label = collection.GetDisplayString();

			bool collectionHighlighted = collectionIndex == selectedCollectionIndex;
			ButtonTheme activeCollectionTheme = GetButtonTheme(true, collectionHighlighted);

			bool collectionPressed = UI.Button(label, activeCollectionTheme, topLeft, new Vector2(width, 2), true, false, false, Anchor.TopLeft, true, 1, isScrolling);
			if (collectionPressed)
			{
				selectedCollectionIndex = collectionIndex;
				selectedChipInCollectionIndex = -1;
				selectedStarredItemIndex = -1;
				lastAutoOpenedCollection = null;
				// If holding control, select without toggling
				if (!InputHelper.CtrlIsHeld) collection.IsToggledOpen = !collection.IsToggledOpen;
			}

			const float nestedInset = 1.75f;

			if (collection.IsToggledOpen)
			{
				for (int chipIndex = 0; chipIndex < collection.Chips.Count; chipIndex++)
				{
					string chipName = collection.Chips[chipIndex];
					ButtonTheme activeChipTheme = collectionIndex == selectedCollectionIndex && chipIndex == selectedChipInCollectionIndex ? ActiveUITheme.ChipLibraryChipToggleOn : ActiveUITheme.ChipLibraryChipToggleOff;
					Vector2 chipLabelPos = new(topLeft.x + nestedInset, UI.PrevBounds.Bottom - UILayoutHelper.DefaultSpacing);
					bool chipPressed = UI.Button(chipName, activeChipTheme, chipLabelPos, new Vector2(width - nestedInset, 2), true, false, false, Anchor.TopLeft, true, 1, isScrolling);
					if (chipPressed)
					{
						bool alreadySelected = selectedChipInCollectionIndex == chipIndex && collectionHighlighted;

						if (alreadySelected) selectedChipInCollectionIndex = -1;
						else
						{
							selectedCollectionIndex = collectionIndex;
							selectedChipInCollectionIndex = chipIndex;
						}

						selectedStarredItemIndex = -1;
						lastAutoOpenedCollection = null;
					}
				}
			}
		}

		static void DrawSelectedItemPanel(Vector2 topLeft, Vector2 size)
        {
            Draw.ID mainPanelID = UI.ReservePanel(); 
            Bounds2D panelBounds = Bounds2D.CreateFromTopLeftAndSize(topLeft, size);
            Bounds2D panelContentBounds = Bounds2D.Shrink(panelBounds, PanelUIPadding);
            Vector2 currentTopLeft = panelContentBounds.TopLeft; 
            const float SectionSpacing = 1.5f;
            const float commentSectionHeight = 8f; 

            bool hasCollectionSelected = selectedCollectionIndex != -1 && selectedChipInCollectionIndex == -1;
            bool hasChipSelected = selectedCollectionIndex != -1 && selectedChipInCollectionIndex != -1;
            bool hasStarredItemSelected = selectedStarredItemIndex != -1;

            currentChipName = ""; 
            currentChipCommentToDraw = ""; 
            bool currentIsCollection = false;
            bool isItemSelected = hasChipSelected || hasCollectionSelected || hasStarredItemSelected;

            using (UI.BeginBoundsScope(true)) 
            {
                Draw.ID topSectionPanelID = UI.ReservePanel(); 

                if (isItemSelected)
                {
                    string header1 = "";
                    string header2 = "";
                    Color header1BGCol = Color.clear, header1TextCol = Color.clear;
                    Color header2BGCol = Color.clear, header2TextCol = Color.clear;
                    bool isCurrentChipStarred = false;

                    if (hasChipSelected) {
                        ChipCollection collection = collections[selectedCollectionIndex];
                        header1 = collection.Name;
                        header2 = collection.Chips[selectedChipInCollectionIndex];
                        currentChipName = header2; currentIsCollection = false;
                        isCurrentChipStarred = project.description.IsStarred(currentChipName, false);
                        ButtonTheme colSourceCollection = GetButtonTheme(true, true); header1BGCol = colSourceCollection.buttonCols.normal; header1TextCol = colSourceCollection.textCols.normal;
                        ButtonTheme colSourceChip = GetButtonTheme(false, true); header2BGCol = colSourceChip.buttonCols.normal; header2TextCol = colSourceChip.textCols.normal;
                    } else if (hasCollectionSelected) {
                        ChipCollection collection = collections[selectedCollectionIndex];
                        header1 = collection.Name;
                        currentChipName = header1; currentIsCollection = true;
                        isCurrentChipStarred = project.description.IsStarred(currentChipName, true);
                        ButtonTheme colSource = GetButtonTheme(true, true); header1BGCol = colSource.buttonCols.normal; header1TextCol = colSource.textCols.normal;
                    } else if (hasStarredItemSelected) {
                        StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
                        header1 = starredItem.Name;
                        currentChipName = header1; currentIsCollection = starredItem.IsCollection;
                        isCurrentChipStarred = true;
                        ButtonTheme colSource = GetButtonTheme(starredItem.IsCollection, true); header1BGCol = colSource.buttonCols.normal; header1TextCol = colSource.textCols.normal;
                    }

                    DrawHeader(header1, header1BGCol, header1TextCol, ref currentTopLeft, panelContentBounds.Width);
                    if (!string.IsNullOrEmpty(header2)) DrawHeader(header2, header2BGCol, header2TextCol, ref currentTopLeft, panelContentBounds.Width);

                    bool toggleStarred = DrawHorizontalButtonGroup(buttonName_starUnstar[isCurrentChipStarred ? 1 : 0], null, ref currentTopLeft, panelContentBounds.Width) == 0;
                    if (toggleStarred) {
                        project.SetStarred(currentChipName, !isCurrentChipStarred, currentIsCollection);
                        if (currentIsCollection) selectedStarredItemIndex = project.description.StarredList.FindIndex(item => item.IsCollection && item.Name == currentChipName);
                        else selectedStarredItemIndex = project.description.StarredList.FindIndex(item => !item.IsCollection && item.Name == currentChipName);

                        if (isCurrentChipStarred) selectedStarredItemIndex = -1; 
                    }

                    if (hasChipSelected) {
                        ChipCollection collection = collections[selectedCollectionIndex];
                        bool canStepUp = selectedChipInCollectionIndex > 0, canStepDown = selectedChipInCollectionIndex < collection.Chips.Count - 1;
                        bool canJumpUpCol = selectedCollectionIndex > 0, canJumpDownCol = selectedCollectionIndex < collections.Count - 1;
                        interactableStates_move[0] = canStepUp || canJumpUpCol; interactableStates_move[1] = canStepDown || canJumpDownCol;
                        int moveStep = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref currentTopLeft, panelContentBounds.Width);
                        int moveJump = DrawHorizontalButtonGroup(buttonNames_jump, interactableStates_move, ref currentTopLeft, panelContentBounds.Width);
                        ChipActionButtons(currentChipName, ref currentTopLeft, panelContentBounds.Width);
                        if (moveStep == 0 || moveJump == 0) MoveSelectedChip(collection, false, moveJump == 0, canStepUp, canJumpUpCol); // Move Up
                        else if (moveStep == 1 || moveJump == 1) MoveSelectedChip(collection, true, moveJump == 1, canStepDown, canJumpDownCol); // Move Down
                    } else if (hasCollectionSelected) {
                        ChipCollection collection = collections[selectedCollectionIndex];
                        interactableStates_move[0] = selectedCollectionIndex > 0; interactableStates_move[1] = selectedCollectionIndex < collections.Count - 1;
                        int moveCol = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref currentTopLeft, panelContentBounds.Width);
                        bool canRenameOrDelete = !ChipDescription.NameMatch(collection.Name, defaultOtherChipsCollectionName);
                        interactableStates_renameDelete[0] = canRenameOrDelete; interactableStates_renameDelete[1] = canRenameOrDelete;
                        int editCol = DrawHorizontalButtonGroup(buttonNames_collectionRenameOrDelete, interactableStates_renameDelete, ref currentTopLeft, panelContentBounds.Width);
                        if (editCol == 0) { UI.GetInputFieldState(ID_NameInput).ClearText(); renamingCollection = true; }
                        else if (editCol == 1) {
                            if (collection.Chips.Count == 0) DeleteSelectedCollection();
                            else { deleteConfirmMessage = $"Delete collection? Chips will move to \"{defaultOtherChipsCollectionName}\"."; deleteConfirmMessageCol = deleteColWarningMedium; isConfirmingCollectionDeletion = true; }
                        }
                        if (moveCol == 0) MoveCollection(true); else if (moveCol == 1) MoveCollection(false);
                    } else if (hasStarredItemSelected) {
                        StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
                        interactableStates_move[0] = selectedStarredItemIndex > 0; interactableStates_move[1] = selectedStarredItemIndex < project.description.StarredList.Count - 1;
                        int moveStar = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref currentTopLeft, panelContentBounds.Width);
                        if (!starredItem.IsCollection) ChipActionButtons(starredItem.Name, ref currentTopLeft, panelContentBounds.Width);
                        if (moveStar == 0 || moveStar == 1) MoveStarredItem(moveStar == 0);
                    }
                }
                MenuHelper.DrawReservedMenuPanel(topSectionPanelID, UI.GetCurrentBoundsScope());

            }


            if (isItemSelected)
            {
                currentTopLeft.y -= SectionSpacing; 


                if (hasChipSelected || (hasStarredItemSelected && !currentIsCollection)) 
                {
                    ChipDescription selectedChipDesc = project.chipLibrary.GetChipDescription(currentChipName);
                    currentChipCommentToDraw = string.IsNullOrEmpty(selectedChipDesc?.ChipComment) ? "No comment available." : selectedChipDesc.ChipComment;
                }
                else 
                {
                    currentChipCommentToDraw = "No comment available.";
                }

                float calculatedHeight = commentTextHeight > 0 ? commentTextHeight + PanelUIPadding : ButtonHeight; 
                float clampedHeight = Mathf.Clamp(calculatedHeight, ButtonHeight, commentSectionHeight);
                Vector2 commentAreaSize = new Vector2(panelContentBounds.Width, clampedHeight);
                Vector2 commentAreaTopLeft = currentTopLeft;


                UI.DrawScrollView(ID_ChipCommentScrollbar, commentAreaTopLeft, commentAreaSize, Anchor.TopLeft, ActiveUITheme.ScrollTheme, drawChipCommentContent);
                currentTopLeft.y = UI.PrevBounds.Bottom - SectionSpacing; 
            }

            Vector2 bottomSectionTopLeft = currentTopLeft;

            if (!(isConfirmingChipDeletion || isConfirmingCollectionDeletion))
            {
                using (UI.BeginBoundsScope(true))
                {
                    Draw.ID bottomSectionPanelID = UI.ReservePanel();

                    if (!renamingCollection)
                    {
                        bool createNew = UI.Button("NEW COLLECTION", ActiveUITheme.ButtonTheme, bottomSectionTopLeft, new Vector2(panelContentBounds.Width, 0), true, false, true, Anchor.TopLeft);
                        if (createNew) creatingNewCollection = true;
                        if (!creatingNewCollection)
                        {
                            bottomSectionTopLeft += Vector2.down * (UI.PrevBounds.Height + DefaultButtonSpacing * 1);
                            bool exit = UI.Button("EXIT LIBRARY", ActiveUITheme.ButtonTheme, bottomSectionTopLeft, new Vector2(panelContentBounds.Width, 0), true, false, true, Anchor.TopLeft);
                            if (exit) ExitLibrary();
                        }
                    }

                    if (creatingNewCollection || renamingCollection)
                    {
                        using (UI.BeginDisabledScope(false))
                        {
                            InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
                            inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;
                            InputFieldState nameField = UI.InputField(ID_NameInput, inputTheme, bottomSectionTopLeft, new Vector2(panelContentBounds.Width, 2.5f), string.Empty, Anchor.TopLeft, 1, ValidateCollectionNameInput, true);
                            int button_cancelConfirm = MenuHelper.DrawButtonPair("CANCEL", renamingCollection ? "RENAME" : "CREATE", UI.PrevBounds.BottomLeft, panelContentBounds.Width, true, true, IsValidCollectionName(nameField.text));
							if (button_cancelConfirm == 0 || (KeyboardShortcuts.CancelShortcutTriggered && (creatingNewCollection || renamingCollection) )) 
                            {
                                nameField.ClearText();
                                creatingNewCollection = false;
                                renamingCollection = false;
                            }
                            else if (button_cancelConfirm == 1 || (KeyboardShortcuts.ConfirmShortcutTriggered && (creatingNewCollection || renamingCollection))) 
                            {
                                if (creatingNewCollection)
                                {
                                    collections.Add(new ChipCollection(nameField.text));
                                    selectedChipInCollectionIndex = -1;
                                    selectedCollectionIndex = collections.Count - 1;
                                    selectedStarredItemIndex = -1;
                                }
                                else if (renamingCollection)
                                {
                                    string nameNew = nameField.text;
                                    project.RenameCollection(selectedCollectionIndex, nameNew);
                                }

                                nameField.ClearText();
                                creatingNewCollection = false;
                                renamingCollection = false;
                            }
                        }
                    }
                    MenuHelper.DrawReservedMenuPanel(bottomSectionPanelID, UI.GetCurrentBoundsScope());
                }
            }


			// Delete confirmation
			if (isConfirmingChipDeletion || isConfirmingCollectionDeletion)
			{
				using (UI.BeginBoundsScope(true))
				{
					using (UI.BeginDisabledScope(false))
					{
						Draw.ID deleteConfirmPanelID = UI.ReservePanel(); // Reserve panel for delete confirmation
						UI.DrawText(deleteConfirmMessage, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, bottomSectionTopLeft, Anchor.TopLeft, deleteConfirmMessageCol); 
						Vector2 deleteButtonTopLeft = UI.PrevBounds.BottomLeft + Vector2.down * DefaultButtonSpacing * 3f; 
						int button_cancelConfirm = MenuHelper.DrawButtonPair("CANCEL", "DELETE", deleteButtonTopLeft, panelContentBounds.Width, false);

						if (button_cancelConfirm == 0) ResetPopupState(); 
						else if (button_cancelConfirm == 1 || (KeyboardShortcuts.ConfirmShortcutTriggered && (isConfirmingChipDeletion || isConfirmingCollectionDeletion))) // confirm delete
						{
							if (isConfirmingChipDeletion) {
                                string chipNameToDelete = currentChipName; // Use the stored currentChipName
								project.DeleteChip(chipNameToDelete);
                                // Adjust selection after deletion
                                if (selectedCollectionIndex != -1 && selectedCollectionIndex < collections.Count) { // Add check for valid index
                                    selectedChipInCollectionIndex = Mathf.Min(selectedChipInCollectionIndex, collections[selectedCollectionIndex].Chips.Count - 1);
                                    if (collections[selectedCollectionIndex].Chips.Count == 0) selectedChipInCollectionIndex = -1;
                                } else if (selectedStarredItemIndex != -1) { // Check if starred item was selected
								    selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
                                    if (project.description.StarredList.Count == 0) selectedStarredItemIndex = -1;
                                } else { // Reset selection if nothing valid remains
									selectedCollectionIndex = -1;
									selectedChipInCollectionIndex = -1;
									selectedStarredItemIndex = -1;
								}
							} else if (isConfirmingCollectionDeletion) {
								DeleteSelectedCollection();
							}
							ResetPopupState();
						}
						MenuHelper.DrawReservedMenuPanel(deleteConfirmPanelID, UI.GetCurrentBoundsScope()); 
					}
				}
			}

			MenuHelper.DrawReservedMenuPanel(mainPanelID, panelBounds, false);

			return; 

			static void DrawHeader(string text, Color bgCol, Color textCol, ref Vector2 topLeft, float width, float spacingBelow = DefaultButtonSpacing) {
				MenuHelper.DrawCentredTextWithBackground(text, topLeft, new Vector2(width, 2), Anchor.TopLeft, textCol, bgCol);
				topLeft += Vector2.down * (UI.PrevBounds.Height + spacingBelow);
			}

			static int DrawHorizontalButtonGroup(string[] names, bool[] interactionStates, ref Vector2 topLeft, float width, float verticalSpacing = DefaultButtonSpacing) {
				int buttonIndex = UI.HorizontalButtonGroup(names, interactionStates, ActiveUITheme.ButtonTheme, topLeft, width, DefaultButtonSpacing, 0, Anchor.TopLeft);
				topLeft.y -= UI.PrevBounds.Height + verticalSpacing;
				return buttonIndex;
			}

			static void ChipActionButtons(string selectedChipName, ref Vector2 topLeft, float width) {
				bool isBuiltin = project.chipLibrary.IsBuiltinChip(selectedChipName);
				interactable_chipActionButtons[0] = project.ViewedChip.CanAddSubchip(selectedChipName);
				interactable_chipActionButtons[1] = !isBuiltin;
				interactable_chipActionButtons[2] = !isBuiltin;
				int chipActionIndex = DrawHorizontalButtonGroup(buttonNames_chipAction, interactable_chipActionButtons, ref topLeft, width);

				if (chipActionIndex == 0) { project.controller.StartPlacing(project.chipLibrary.GetChipDescription(selectedChipName)); ExitLibrary(); }
				else if (chipActionIndex == 1) {
					chipToOpenName = selectedChipName;
					if (project.ActiveChipHasUnsavedChanges()) UnsavedChangesPopup.OpenPopup(OpenChipIfConfirmed);
					else OpenChipIfConfirmed(true);
				} else if (chipActionIndex == 2) {
					isConfirmingChipDeletion = true;
					(string msg, bool warn) = CreateDeleteConfirmationMessage(selectedChipName);
					deleteConfirmMessage = msg; deleteConfirmMessageCol = warn ? deleteColWarningHigh : deleteColWarningMedium;
				}
			}

            static void MoveSelectedChip(ChipCollection currentCollection, bool moveDown, bool jump, bool canStep, bool canJumpCollection) {
                 bool moveWithinCurrentCollection = (canStep && !jump) || (jump && !canJumpCollection);
                 string chipName = currentCollection.Chips[selectedChipInCollectionIndex]; 

                 if (moveWithinCurrentCollection) {
                     int startIndex = selectedChipInCollectionIndex;
                     int endIndex = jump ? (moveDown ? currentCollection.Chips.Count - 1 : 0) : (selectedChipInCollectionIndex + (moveDown ? 1 : -1));
                     currentCollection.Chips.RemoveAt(startIndex);
                     currentCollection.Chips.Insert(endIndex, chipName);
                     selectedChipInCollectionIndex = endIndex;
                 } else {
                     currentCollection.Chips.RemoveAt(selectedChipInCollectionIndex); 
                     MoveSelectedChipToNewCollection(selectedCollectionIndex + (moveDown ? 1 : -1), chipName);
                 }
            }

             static void MoveCollection(bool moveUp) {
                 int indexStart = selectedCollectionIndex;
                 int indexEnd = selectedCollectionIndex + (moveUp ? -1 : 1);
                 (collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
                 selectedCollectionIndex = indexEnd;
             }

             static void MoveStarredItem(bool moveUp) {
                 int fromIndex = selectedStarredItemIndex;
                 int targetIndex = selectedStarredItemIndex + (moveUp ? -1 : 1);
                 (project.description.StarredList[fromIndex], project.description.StarredList[targetIndex]) = (project.description.StarredList[targetIndex], project.description.StarredList[fromIndex]);
                 selectedStarredItemIndex = targetIndex;
             }

             static ChipCollection MoveSelectedChipToNewCollection(int newCollectionIndex, string chipName) // Added chipName parameter
             {

                 ChipCollection collectionOld = collections[selectedCollectionIndex];
                 if (collectionOld == lastAutoOpenedCollection) {
                     lastAutoOpenedCollection = null;
                     collectionOld.IsToggledOpen = false;
                 }

                 bool movingUp = newCollectionIndex < selectedCollectionIndex;
                 selectedCollectionIndex = newCollectionIndex;
                 ChipCollection collectionNew = collections[selectedCollectionIndex];

                 if (movingUp) {
                     collectionNew.Chips.Add(chipName);
                     selectedChipInCollectionIndex = collectionNew.Chips.Count - 1;
                 } else {
                     collectionNew.Chips.Insert(0, chipName);
                     selectedChipInCollectionIndex = 0;
                 }

                 if (!collectionNew.IsToggledOpen) {
                     lastAutoOpenedCollection = collectionNew;
                     collectionNew.IsToggledOpen = true;
                 }

                 return collectionNew;
             }
		}

        static void DrawChipCommentContent(Vector2 topLeft, float width, bool isLayoutPass)
        {
            if (!string.IsNullOrEmpty(currentChipCommentToDraw))
            {
                FontType font = ActiveUITheme.FontRegular;
                float fontSize = ActiveUITheme.FontSizeRegular * 0.9f;
                Color commentColor = currentChipCommentToDraw == "No comment available." ? Color.gray * 0.8f : Color.white * 0.8f;

                string wrappedText = ChipLibraryMenu.WrapText(currentChipCommentToDraw, font, fontSize, width);

                Seb.Vis.Text.Rendering.TextRenderer.BoundingBox bounds = Draw.CalculateTextBounds(wrappedText, font, fontSize, topLeft, Anchor.TopLeft);
                commentTextHeight = bounds.Size.y; 

                if (!isLayoutPass)
                {
                    UI.DrawText(wrappedText, font, fontSize, topLeft, Anchor.TopLeft, commentColor);
                }

                UI.OverridePreviousBounds(Bounds2D.CreateFromTopLeftAndSize(topLeft, new Vector2(width, commentTextHeight)));
            }
            else
            {
                 commentTextHeight = 0;
                 UI.OverridePreviousBounds(Bounds2D.CreateFromTopLeftAndSize(topLeft, new Vector2(width, 0))); // Set zero height bounds
            }
        }

        // Manual text wrapping helper function - Made public static
        public static string WrapText(string text, FontType font, float fontSize, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            {
                return text;
            }

            wrapStringBuilder.Clear();
            string[] words = text.Split(' ');
            string currentLine = "";

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (word.Contains('\n'))
                {
                    string[] parts = word.Split('\n');
                    for(int p=0; p<parts.Length; p++)
                    {
                        string part = parts[p];
                         string testLinePart = string.IsNullOrEmpty(currentLine) ? part : currentLine + " " + part;
                         Vector2 testPartSize = Draw.CalculateTextBoundsSize(testLinePart, fontSize, font);

                         if (testPartSize.x <= maxWidth)
                         {
                             currentLine = testLinePart;
                         }
                         else
                         {
                            if (!string.IsNullOrEmpty(currentLine)) wrapStringBuilder.AppendLine(currentLine);
                             currentLine = part; 
                         }

                        if (p < parts.Length - 1)
                        {
                            if (!string.IsNullOrEmpty(currentLine)) wrapStringBuilder.AppendLine(currentLine);
                             currentLine = ""; 
                        }
                    }
                    continue; 
                }


                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                Vector2 testLineSize = Draw.CalculateTextBoundsSize(testLine, fontSize, font);

                if (testLineSize.x <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {

                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        wrapStringBuilder.AppendLine(currentLine);
                    }

                    if (Draw.CalculateTextBoundsSize(word, fontSize, font).x > maxWidth)
                    {
                        string currentWordPart = "";
                        for(int j = 0; j < word.Length; j++)
                        {
                            string testWordPart = currentWordPart + word[j];
                             if (Draw.CalculateTextBoundsSize(testWordPart, fontSize, font).x <= maxWidth)
                             {
                                 currentWordPart = testWordPart;
                             }
                             else
                             {
                                 wrapStringBuilder.AppendLine(currentWordPart);
                                 currentWordPart = word[j].ToString();
                             }
                        }
                         currentLine = currentWordPart;
                    }
                    else
                    {
                        currentLine = word;
                    }
                }
            }

            // Append the last line
            if (!string.IsNullOrEmpty(currentLine))
            {
                wrapStringBuilder.Append(currentLine);
            }

            return wrapStringBuilder.ToString();
        }


		public static void OnMenuOpened()
		{
			wasOpenedThisFrame = true;
			// Ensure the mandatory "OTHER" collection exists
			if (GetDefaultCollection() == null)
			{
				collections.Add(new ChipCollection(defaultOtherChipsCollectionName));
			}


			// Automatically add any chips not in a collection to the "other" collection
			HashSet<string> chipsInCollection = new(collections.SelectMany(c => c.Chips), ChipDescription.NameComparer);
			ChipCollection defaultCollection = GetDefaultCollection();

            if (defaultCollection != null)
            {
                foreach (ChipDescription chip in project.chipLibrary.allChips)
                {
                    if (!project.chipLibrary.IsBuiltinChip(chip.Name) && !chipsInCollection.Contains(chip.Name))
                    {
                        if (!defaultCollection.Chips.Contains(chip.Name, ChipDescription.NameComparer))
                        {
						    defaultCollection.Chips.Add(chip.Name);
                        }
                    }
                }
                List<string> chipsToRemoveFromOther = new List<string>();
                foreach (string chipName in defaultCollection.Chips)
                {
                    bool foundElsewhere = false;
                    foreach(var collection in collections)
                    {
                        if (collection != defaultCollection && collection.Chips.Contains(chipName, ChipDescription.NameComparer))
                        {
                            foundElsewhere = true;
                            break;
                        }
                    }
                    if (foundElsewhere)
                    {
                        chipsToRemoveFromOther.Add(chipName);
                    }
                }
                foreach(var chipToRemove in chipsToRemoveFromOther)
                {
                    defaultCollection.Chips.RemoveAll(c => ChipDescription.NameMatch(c, chipToRemove));
                }
            }


			// Reset state
			ResetPopupState();

			selectedStarredItemIndex = Mathf.Clamp(selectedStarredItemIndex, -1, project.description.StarredList.Count - 1);
            selectedCollectionIndex = Mathf.Clamp(selectedCollectionIndex, 0, collections.Count - 1); 
            if (selectedCollectionIndex >= 0 && collections.Count > 0 && selectedCollectionIndex < collections.Count)
            {
                selectedChipInCollectionIndex = Mathf.Clamp(selectedChipInCollectionIndex, -1, collections[selectedCollectionIndex].Chips.Count - 1);
            } else {
				selectedChipInCollectionIndex = -1;
			}
		}

		public static void Reset()
		{
			selectedStarredItemIndex = -1;
			selectedCollectionIndex = 0;
			selectedChipInCollectionIndex = -1;
			ResetPopupState();
		}

		static void DeleteSelectedCollection()
		{
			ChipCollection defaultCollection = GetDefaultCollection();
			ChipCollection collectionToDelete = collections[selectedCollectionIndex];

			if (defaultCollection == null) {
				defaultCollection = new ChipCollection(defaultOtherChipsCollectionName);
				collections.Add(defaultCollection);
			}


			foreach (string chipName in collectionToDelete.Chips)
			{
                if (!defaultCollection.Chips.Contains(chipName, ChipDescription.NameComparer))
                {
				    defaultCollection.Chips.Add(chipName);
                }
			}

			project.SetStarred(collectionToDelete.Name, false, true, false);
			collections.RemoveAt(selectedCollectionIndex);
			selectedCollectionIndex = Mathf.Max(0, selectedCollectionIndex - 1);
            selectedChipInCollectionIndex = -1; 

			project.SaveCurrentProjectDescription();
		}

		static ChipCollection GetDefaultCollection() => collections.FirstOrDefault(c => ChipDescription.NameMatch(c.Name, defaultOtherChipsCollectionName));

		static bool ValidateCollectionNameInput(string name)
		{
			return name.Length <= 24;
		}

		static bool IsValidCollectionName(string name)
		{
			if (!ValidateCollectionNameInput(name)) return false;

			if (string.IsNullOrWhiteSpace(name)) return false;

			for (int i = 0; i < collections.Count; i++)
			{
				if (renamingCollection && i == selectedCollectionIndex && ChipDescription.NameMatch(collections[i].Name, name)) continue;
                if (ChipDescription.NameMatch(collections[i].Name, name)) return false;

			}

			return true;
		}

		static ButtonTheme GetButtonTheme(bool isCollection, bool isSelected) =>
			isCollection
				? isSelected ? ActiveUITheme.ChipLibraryCollectionToggleOn : ActiveUITheme.ChipLibraryCollectionToggleOff
				: isSelected
					? ActiveUITheme.ChipLibraryChipToggleOn
					: ActiveUITheme.ChipLibraryChipToggleOff;

		static void OpenChipIfConfirmed(bool confirm)
		{
			if (confirm)
			{
				project.LoadDevChipOrCreateNewIfDoesntExist(chipToOpenName);
				ExitLibrary();
			}
			else
			{
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipLibrary);
			}
		}

		static void ExitLibrary()
		{
			project.UpdateAndSaveProjectDescription();
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static (string msg, bool warn) CreateDeleteConfirmationMessage(string chipName)
		{
			List<string> parentNames = project.chipLibrary.GetDirectParentChips(chipName).Select(c => c.Name).ToList();
			bool usedInCurrentChip = Project.ActiveProject.ViewedChip.GetSubchips().Any(s => s.Description.NameMatch(chipName));

			if (usedInCurrentChip)
			{
				parentNames.Remove(Project.ActiveProject.ViewedChip.ChipName);
				parentNames.Insert(0, "the CURRENT CHIP");
			}

			string message = "Are you sure you want to delete this chip? ";
			bool warn = parentNames.Count > 0;

			if (Project.ActiveProject.ViewedChip.LastSavedDescription?.NameMatch(chipName) == true)
			{
				message = "Are you sure you want to delete the chip that you are CURRENTLY EDITING? ";
				warn = true;
			}

			if (parentNames.Count == 0) message += "It is not used anywhere.";
			else message += CreateChipInUseWarningMessage(parentNames);

			string formattedMessage = UI.LineBreakByCharCount(message, deleteMessageMaxCharsPerLine);
			return (formattedMessage, warn);

			string CreateChipInUseWarningMessage(List<string> chipsUsingCurrentChip)
			{
				int numUses = chipsUsingCurrentChip.Count;
				string usage = "It is used by";
				if (numUses == 1) return $"{usage} {FormatChipName(0)}.";
				if (numUses == 2) return $"{usage} {FormatChipName(0)} and {FormatChipName(1)}.";
				if (numUses > 2) return $"{usage} {FormatChipName(0)} and {numUses - 1} others.";
				return string.Empty;

				string FormatChipName(int index)
				{
					bool useQuotes = !(index == 0 && usedInCurrentChip);
					string formatted = useQuotes ? $"\"{chipsUsingCurrentChip[index]}\"" : chipsUsingCurrentChip[index];
					return formatted;
				}
			}
		}
	}
}