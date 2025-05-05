using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
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

		static readonly Color deleteColWarningHigh = new(0.95f, 0.35f, 0.35f);
		static readonly Color deleteColWarningMedium = new(1f, 0.75f, 0.2f);

		// if chip is moved to another collection, it will be auto-opened. Keep track so it can be auto-closed if chip is then moved out of that collection ('just passing through')
		static ChipCollection lastAutoOpenedCollection;

		static List<ChipCollection> collections => project.description.ChipCollections;

		static Project project => Project.ActiveProject;

		public static void DrawMenu()
		{
			MenuHelper.DrawBackgroundOverlay();
			Vector2 panelEdgePadding = new(3.25f, 2.6f);

			const float interPanelSpacing = 1.5f;
			const float menuOffsetY = 1.13f;
			const float starredPanelWidthT = 0.35f;
			const float collectionPanelWidthT = 0.37f;
			const float selectedPanelWidthT = 1 - (starredPanelWidthT + collectionPanelWidthT);

			float panelWidthSum = UI.Width - interPanelSpacing * 2 - panelEdgePadding.x * 2;
			float panelHeight = UI.Height - panelEdgePadding.y * 2;

			Vector2 panelATopLeft = UI.TopLeft + new Vector2(panelEdgePadding.x, -panelEdgePadding.y + menuOffsetY);
			Vector2 panelSizeA = new(panelWidthSum * starredPanelWidthT, panelHeight);
			Vector2 panelBTopLeft = panelATopLeft + Vector2.right * (panelSizeA.x + interPanelSpacing);
			Vector2 panelSizeB = new(panelWidthSum * collectionPanelWidthT, panelHeight);
			Vector2 panelCTopLeft = panelBTopLeft + Vector2.right * (panelSizeB.x + interPanelSpacing);
			Vector2 panelSizeC = new(panelWidthSum * selectedPanelWidthT, panelHeight);

			isScrolling = UI.GetScrollbarState(ID_CollectionsScrollbar).isDragging || UI.GetScrollbarState(ID_StarredScrollbar).isDragging;

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

			interactableStates_starredList[0] = index < project.description.StarredList.Count - 1; // can move down
			interactableStates_starredList[1] = index > 0; // can move up

			bool entryPressed = UI.Button(starredItem.Name, theme, topLeft, new Vector2(width, 2), true, false, false, Anchor.TopLeft, true, 1, isScrolling);
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
			Draw.ID panelID = UI.ReservePanel();
			Bounds2D panelContentBounds = Bounds2D.Shrink(Bounds2D.CreateFromTopLeftAndSize(topLeft, size), PanelUIPadding);
			topLeft = panelContentBounds.TopLeft;
			const float SectionSpacing = 3;

			bool hasCollectionSelected = selectedCollectionIndex != -1 && selectedChipInCollectionIndex == -1;
			bool hasChipSelected = selectedCollectionIndex != -1 && selectedChipInCollectionIndex != -1;
			bool hasStarredItemSelected = selectedStarredItemIndex != -1;

			if (hasChipSelected || hasCollectionSelected || hasStarredItemSelected)
			{
				using (UI.BeginBoundsScope(true))
				{
					// ---- Selected Chip UI ----
					if (hasChipSelected)
					{
						// ---- Draw ----
						ChipCollection collection = collections[selectedCollectionIndex];
						string selectedChipName = collection.Chips[selectedChipInCollectionIndex];
						bool canStepUpInCollection = selectedChipInCollectionIndex > 0;
						bool canStepDownInCollection = selectedChipInCollectionIndex < collection.Chips.Count - 1;
						bool canJumpUpACollection = selectedCollectionIndex > 0;
						bool canJumpDownACollection = selectedCollectionIndex < collections.Count - 1;

						ButtonTheme colSource = GetButtonTheme(false, true);
						ButtonTheme colSourceCollection = GetButtonTheme(true, true);
						DrawHeader(collection.Name, colSourceCollection.buttonCols.normal, colSourceCollection.textCols.normal, ref topLeft, panelContentBounds.Width);
						DrawHeader(selectedChipName, colSource.buttonCols.normal, colSource.textCols.normal, ref topLeft, panelContentBounds.Width);

						bool isStarred = project.description.IsStarred(selectedChipName, false);
						bool toggleStarred = DrawHorizontalButtonGroup(buttonName_starUnstar[isStarred ? 1 : 0], null, ref topLeft, panelContentBounds.Width) == 0;
						// Buttons: down/up
						interactableStates_move[0] = canStepUpInCollection || canJumpUpACollection;
						interactableStates_move[1] = canStepDownInCollection || canJumpDownACollection;
						int buttonIndex_moveStep = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref topLeft, panelContentBounds.Width);
						int buttonIndex_moveJump = DrawHorizontalButtonGroup(buttonNames_jump, interactableStates_move, ref topLeft, panelContentBounds.Width);
						ChipActionButtons(selectedChipName, ref topLeft, panelContentBounds.Width);

						bool moveSingleStepDown = buttonIndex_moveStep == 1;
						bool moveJumpDown = buttonIndex_moveJump == 1;
						bool moveSingleStepUp = buttonIndex_moveStep == 0;
						bool moveJumpUp = buttonIndex_moveJump == 0;

						// ---- Handle button inputs ----
						if (toggleStarred)
						{
							project.SetStarred(selectedChipName, !isStarred, false);
						}

						if (moveSingleStepDown || moveJumpDown) // Move chip down
						{
							bool moveWithinCurrentCollection = (moveSingleStepDown && canStepDownInCollection) || (moveJumpDown && !canJumpDownACollection);
							if (moveWithinCurrentCollection) // move down in current collection
							{
								int targetIndex = moveJumpDown ? collection.Chips.Count - 1 : selectedChipInCollectionIndex + 1;
								collection.Chips.RemoveAt(selectedChipInCollectionIndex);
								collection.Chips.Insert(targetIndex, selectedChipName);
								selectedChipInCollectionIndex = targetIndex;
							}
							else // move down to next collection
							{
								collection = MoveSelectedChipToNewCollection(selectedCollectionIndex + 1);
							}
						}
						else if (moveSingleStepUp || moveJumpUp) // Move chip up
						{
							bool moveWithinCurrentCollection = (moveSingleStepUp && canStepUpInCollection) || (moveJumpUp && !canJumpUpACollection);
							if (moveWithinCurrentCollection) // move up in current collection
							{
								int targetIndex = moveJumpUp ? 0 : selectedChipInCollectionIndex - 1;
								collection.Chips.RemoveAt(selectedChipInCollectionIndex);
								collection.Chips.Insert(targetIndex, selectedChipName);
								selectedChipInCollectionIndex = targetIndex;
							}
							else // move up to next collection
							{
								collection = MoveSelectedChipToNewCollection(selectedCollectionIndex - 1);
							}
						}
					}
					// ---- Selected Collection UI ----
					else if (hasCollectionSelected)
					{
						// ---- Draw ----
						ChipCollection collection = collections[selectedCollectionIndex];
						ButtonTheme colSource = GetButtonTheme(true, true);
						DrawHeader(collection.Name, colSource.buttonCols.normal, colSource.textCols.normal, ref topLeft, panelContentBounds.Width);

						bool isStarred = project.description.IsStarred(collection.Name, true);
						bool toggleStarred = DrawHorizontalButtonGroup(buttonName_starUnstar[isStarred ? 1 : 0], null, ref topLeft, panelContentBounds.Width) == 0;

						interactableStates_move[0] = selectedCollectionIndex > 0;
						interactableStates_move[1] = selectedCollectionIndex < collections.Count - 1;
						int buttonIndexOrganize = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref topLeft, panelContentBounds.Width);

						bool canRenameOrDelete = !ChipDescription.NameMatch(collection.Name, defaultOtherChipsCollectionName);
						interactableStates_renameDelete[0] = canRenameOrDelete;
						interactableStates_renameDelete[1] = canRenameOrDelete;
						int buttonIndexEditCollection = DrawHorizontalButtonGroup(buttonNames_collectionRenameOrDelete, interactableStates_renameDelete, ref topLeft, panelContentBounds.Width);

						// ---- Handle button inputs ----
						if (toggleStarred)
						{
							project.SetStarred(collection.Name, !isStarred, true);
						}

						if (buttonIndexEditCollection == 0) // Rename collection
						{
							UI.GetInputFieldState(ID_NameInput).ClearText();
							renamingCollection = true;
						}
						else if (buttonIndexEditCollection == 1) // Delete collection
						{
							if (collection.Chips.Count == 0) DeleteSelectedCollection();
							else
							{
								deleteConfirmMessage = $"Are you sure you want to delete this collection? The chips inside of it will be moved to \"{defaultOtherChipsCollectionName}\".";
								deleteConfirmMessage = UI.LineBreakByCharCount(deleteConfirmMessage, deleteMessageMaxCharsPerLine);
								deleteConfirmMessageCol = deleteColWarningMedium;
								isConfirmingCollectionDeletion = true;
							}
						}

						if (buttonIndexOrganize == 0) // Move collection up
						{
							int indexStart = selectedCollectionIndex;
							int indexEnd = selectedCollectionIndex - 1;
							(collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
							selectedCollectionIndex = indexEnd;
							collection = collections[selectedCollectionIndex];
						}
						else if (buttonIndexOrganize == 1) // Move collection down
						{
							int indexStart = selectedCollectionIndex;
							int indexEnd = selectedCollectionIndex + 1;
							(collections[indexStart], collections[indexEnd]) = (collections[indexEnd], collections[indexStart]);
							selectedCollectionIndex = indexEnd;
							collection = collections[selectedCollectionIndex];
						}
					}
					else if (hasStarredItemSelected)
					{
						StarredItem starredItem = project.description.StarredList[selectedStarredItemIndex];
						ButtonTheme colSource = GetButtonTheme(starredItem.IsCollection, true);
						DrawHeader(starredItem.Name, colSource.buttonCols.normal, colSource.textCols.normal, ref topLeft, panelContentBounds.Width);

						bool removeStarred = DrawHorizontalButtonGroup(buttonName_starUnstar[1], null, ref topLeft, panelContentBounds.Width) == 0;
						// Buttons: move down/up
						interactableStates_move[0] = selectedStarredItemIndex > 0; // can move up
						interactableStates_move[1] = selectedStarredItemIndex < project.description.StarredList.Count - 1; // can move down
						int buttonIndexOrganize = DrawHorizontalButtonGroup(buttonNames_moveSingleStep, interactableStates_move, ref topLeft, panelContentBounds.Width);

						if (!starredItem.IsCollection)
						{
							ChipActionButtons(starredItem.Name, ref topLeft, panelContentBounds.Width);
						}

						if (buttonIndexOrganize == 0 || buttonIndexOrganize == 1) // move down/up
						{
							int fromIndex = selectedStarredItemIndex;
							int targetIndex = buttonIndexOrganize == 0 ? fromIndex - 1 : fromIndex + 1;
							(project.description.StarredList[fromIndex], project.description.StarredList[targetIndex]) = (project.description.StarredList[targetIndex], project.description.StarredList[fromIndex]);
							selectedStarredItemIndex = targetIndex;
						}
						else if (removeStarred)
						{
							project.SetStarred(starredItem.Name, false, starredItem.IsCollection);
							selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
						}
					}

					topLeft = UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * SectionSpacing;
					MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());
				}
			}

			if (!(isConfirmingChipDeletion || isConfirmingCollectionDeletion))
			{
				using (UI.BeginBoundsScope(true))
				{
					panelID = UI.ReservePanel();

					// New collection button
					if (!renamingCollection)
					{
						bool createNew = UI.Button("NEW COLLECTION", ActiveUITheme.ButtonTheme, topLeft, new Vector2(panelContentBounds.Width, 0), true, false, true, Anchor.TopLeft);
						if (createNew) creatingNewCollection = true;
						if (!creatingNewCollection)
						{
							topLeft += Vector2.down * (UI.PrevBounds.Height + DefaultButtonSpacing * 1);
							bool exit = UI.Button("EXIT LIBRARY", ActiveUITheme.ButtonTheme, topLeft, new Vector2(panelContentBounds.Width, 0), true, false, true, Anchor.TopLeft);
							if (exit) ExitLibrary();
						}

						topLeft += Vector2.down * (UI.PrevBounds.Height + DefaultButtonSpacing * 2);
					}

					// New collection / rename collection input field
					if (creatingNewCollection || renamingCollection)
					{
						using (UI.BeginDisabledScope(false))
						{
							InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
							inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;
							InputFieldState nameField = UI.InputField(ID_NameInput, inputTheme, topLeft, new Vector2(panelContentBounds.Width, 2.5f), string.Empty, Anchor.TopLeft, 1, ValidateCollectionNameInput, true);
							int button_cancelConfirm = MenuHelper.DrawButtonPair("CANCEL", renamingCollection ? "RENAME" : "CREATE", UI.PrevBounds.BottomLeft, panelContentBounds.Width, true, true, IsValidCollectionName(nameField.text));
							if (button_cancelConfirm == 0)
							{
								nameField.ClearText();
								creatingNewCollection = false;
								renamingCollection = false;
							}
							else if (button_cancelConfirm == 1 || KeyboardShortcuts.ConfirmShortcutTriggered)
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

					topLeft = UI.GetCurrentBoundsScope().BottomLeft + Vector2.down * SectionSpacing;
					MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());
				}
			}

			// Delete confirmation
			if (isConfirmingChipDeletion || isConfirmingCollectionDeletion)
			{
				using (UI.BeginBoundsScope(true))
				{
					using (UI.BeginDisabledScope(false))
					{
						panelID = UI.ReservePanel();
						UI.DrawText(deleteConfirmMessage, ActiveUITheme.FontRegular, ActiveUITheme.FontSizeRegular, topLeft, Anchor.TopLeft, deleteConfirmMessageCol);
						topLeft += Vector2.down * (UI.PrevBounds.Height + DefaultButtonSpacing * 3f);
						int button_cancelConfirm = MenuHelper.DrawButtonPair("CANCEL", "DELETE", topLeft, panelContentBounds.Width, false);

						if (button_cancelConfirm == 0) // cancel delete
						{
							ResetPopupState();
						}
						else if (button_cancelConfirm == 1 || KeyboardShortcuts.ConfirmShortcutTriggered) // confirm delete
						{
							if (isConfirmingChipDeletion)
							{
								if (selectedCollectionIndex != -1) // deleting from collection
								{
									ChipCollection collection = collections[selectedCollectionIndex];
									string chipName = collection.Chips[selectedChipInCollectionIndex];
									project.DeleteChip(chipName);
									selectedChipInCollectionIndex = Mathf.Min(selectedChipInCollectionIndex, collection.Chips.Count - 1);
								}
								else // deleting chip
								{
									string chipName = project.description.StarredList[selectedStarredItemIndex].Name;
									project.DeleteChip(chipName);
									selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
								}
							}
							else if (isConfirmingCollectionDeletion)
							{
								DeleteSelectedCollection();
							}

							ResetPopupState();
						}

						MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());
					}
				}
			}

			return;

			static void DrawHeader(string text, Color bgCol, Color textCol, ref Vector2 topLeft, float width, float spacingBelow = DefaultButtonSpacing)
			{
				MenuHelper.DrawCentredTextWithBackground(text, topLeft, new Vector2(width, 2), Anchor.TopLeft, textCol, bgCol);
				topLeft += Vector2.down * (UI.PrevBounds.Height + spacingBelow);
			}

			static int DrawHorizontalButtonGroup(string[] names, bool[] interactionStates, ref Vector2 topLeft, float width, float verticalSpacing = DefaultButtonSpacing)
			{
				int buttonIndex = UI.HorizontalButtonGroup(names, interactionStates, ActiveUITheme.ButtonTheme, topLeft, width, DefaultButtonSpacing, 0, Anchor.TopLeft);
				topLeft.y -= UI.PrevBounds.Height + verticalSpacing;
				return buttonIndex;
			}

			static void ChipActionButtons(string selectedChipName, ref Vector2 topLeft, float width)
			{
				bool isBuiltin = project.chipLibrary.IsBuiltinChip(selectedChipName);
				interactable_chipActionButtons[0] = project.ViewedChip.CanAddSubchip(selectedChipName);
				interactable_chipActionButtons[1] = !isBuiltin;
				interactable_chipActionButtons[2] = !isBuiltin;
				int chipActionIndex = DrawHorizontalButtonGroup(buttonNames_chipAction, interactable_chipActionButtons, ref topLeft, width);

				if (chipActionIndex == 0) // use
				{
					project.controller.StartPlacing(project.chipLibrary.GetChipDescription(selectedChipName));
					ExitLibrary();
				}
				else if (chipActionIndex == 1) // open
				{
					chipToOpenName = selectedChipName;
					if (project.ActiveChipHasUnsavedChanges())
					{
						UnsavedChangesPopup.OpenPopup(OpenChipIfConfirmed);
					}
					else
					{
						OpenChipIfConfirmed(true);
					}
				}
				else if (chipActionIndex == 2) // delete
				{
					isConfirmingChipDeletion = true;
					(string msg, bool warn) = CreateDeleteConfirmationMessage(selectedChipName);
					deleteConfirmMessage = msg;
					deleteConfirmMessageCol = warn ? deleteColWarningHigh : deleteColWarningMedium;
				}
			}

			static ChipCollection MoveSelectedChipToNewCollection(int newCollectionIndex)
			{
				ChipCollection collectionOld = collections[selectedCollectionIndex];
				string chipName = collectionOld.Chips[selectedChipInCollectionIndex];

				collectionOld.Chips.RemoveAt(selectedChipInCollectionIndex);
				// If this collection was opened automatically when the chip was moved to it previously, close it automatically now that chip is leaving it
				if (collectionOld == lastAutoOpenedCollection)
				{
					lastAutoOpenedCollection = null;
					collectionOld.IsToggledOpen = false;
				}

				bool movingUp = newCollectionIndex < selectedCollectionIndex;
				selectedCollectionIndex = newCollectionIndex;
				ChipCollection collectionNew = collections[selectedCollectionIndex];

				if (movingUp)
				{
					collectionNew.Chips.Add(chipName);
					selectedChipInCollectionIndex = collectionNew.Chips.Count - 1;
				}
				else
				{
					collectionNew.Chips.Insert(0, chipName);
					selectedChipInCollectionIndex = 0;
				}

				if (!collectionNew.IsToggledOpen)
				{
					lastAutoOpenedCollection = collectionNew;
					collectionNew.IsToggledOpen = true;
				}

				return collectionNew;
			}
		}


		public static void OnMenuOpened()
		{
			wasOpenedThisFrame = true;
			//Debug.Log("Overriding chip collections with defaults");
			//Project.ActiveProject.description.ChipCollections = new(Main.CreateDefaultChipCollections());

			// Ensure the mandatory "OTHER" collection exists
			if (GetDefaultCollection() == null)
			{
				collections.Add(new ChipCollection(defaultOtherChipsCollectionName));
			}


			// Automatically add any chips not in a collection to the "other" collection
			HashSet<string> chipsInCollection = new(collections.SelectMany(c => c.Chips), ChipDescription.NameComparer);
			ChipCollection defaultCollection = GetDefaultCollection();

			foreach (ChipDescription chip in project.chipLibrary.allChips)
			{
				if (!chipsInCollection.Contains(chip.Name))
				{
					defaultCollection.Chips.Add(chip.Name);
				}
			}

			// Reset state
			ResetPopupState();

			selectedStarredItemIndex = Mathf.Min(selectedStarredItemIndex, project.description.StarredList.Count - 1);
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

			foreach (string chipName in collectionToDelete.Chips)
			{
				defaultCollection.Chips.Add(chipName);
			}

			project.SetStarred(collectionToDelete.Name, false, true, false);
			collections.RemoveAt(selectedCollectionIndex);
			selectedCollectionIndex = Mathf.Max(0, selectedCollectionIndex - 1);

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
				ChipCollection collection = collections[i];
				if (i == selectedCollectionIndex) continue;

				if (ChipDescription.NameMatch(collection.Name, name)) return false;
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