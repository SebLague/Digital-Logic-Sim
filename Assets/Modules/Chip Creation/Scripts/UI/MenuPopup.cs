using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation.UI
{
	public class MenuPopup : MonoBehaviour
	{

		[SerializeField] Button menuButton;
		[SerializeField] RectTransform menuPopup;
		[SerializeField] ProjectManager chipCreationManager;
		[SerializeField] CustomButton newButton;
		[SerializeField] CustomButton saveButton;
		[SerializeField] CustomButton libraryButton;
		[SerializeField] CustomButton optionsButton;
		[SerializeField] CustomButton quitButton;
		[SerializeField] SaveMenu saveMenu;
		[SerializeField] ChipLibraryMenu libraryMenu;
		[SerializeField] EditorOptionsMenu optionsMenu;
		[SerializeField] ConfirmationPopup confirmationPopup;

		RectTransform menuButtonRect;

		void Start()
		{
			menuButtonRect = menuButton.GetComponent<RectTransform>();
			menuButton.onClick.AddListener(ToggleMenuVisibility);

			newButton.ButtonClicked += CreateNewChip;
			saveButton.ButtonClicked += OpenSaveMenu;
			libraryButton.ButtonClicked += OpenLibrary;
			optionsButton.ButtonClicked += OpenOptionsMenu;
			quitButton.ButtonClicked += Quit;

			Close();
		}

		void Update()
		{
			HandleMouseInput();
			HandleKeyboardShortcuts();
		}

		void HandleMouseInput()
		{
			if (MenuIsOpen())
			{
				// Close menu if clicking off menu
				if (Mouse.current.leftButton.wasPressedThisFrame)
				{
					if (!UIHelper.MouseOverRect(menuPopup) && !UIHelper.MouseOverRect(menuButtonRect))
					{
						Close();
					}
				}
			}
		}

		void HandleKeyboardShortcuts()
		{
			Keyboard keyboard = Keyboard.current;

			if (chipCreationManager.ActiveViewChipEditor.CanEdit)
			{
				if (keyboard.ctrlKey.isPressed)
				{

					// Save shortcut
					if (keyboard.sKey.wasPressedThisFrame && !libraryMenu.IsOpen())
					{
						OpenSaveMenu();
					}
					// Library shortcut
					if (keyboard.lKey.wasPressedThisFrame && !saveMenu.IsOpen())
					{
						OpenLibrary();
					}
					// New chip shortcut
					if (keyboard.nKey.wasPressedThisFrame && !saveMenu.IsOpen())
					{
						CreateNewChip();
					}
					// Quit shortcut
					if (keyboard.qKey.wasPressedThisFrame)
					{
						Quit();
					}
				}
			}

			// Close all menus shortcut
			if (keyboard.escapeKey.wasPressedThisFrame)
			{
				CloseAll();
			}
		}

		void CreateNewChip()
		{
			CloseAll();

			ChipEditor chipEditor = chipCreationManager.ActiveEditChipEditor;

			if (chipEditor.HasUnsavedChanges())
			{
				string message = "Are you sure you want to create a new chip? The current chip has unsaved changes which will be lost.";
				confirmationPopup.Open(message, "CANCEL", "CREATE NEW", null, ConfirmCreateNewChip);
			}
			else
			{
				ConfirmCreateNewChip();
			}

		}

		void ConfirmCreateNewChip()
		{
			chipCreationManager.OpenNewChipEditor();
		}

		void OpenSaveMenu()
		{
			Close();
			saveMenu.Open(chipCreationManager.ActiveEditChipEditor);
		}

		void OpenLibrary()
		{
			Close();
			libraryMenu.Open();
		}

		void OpenOptionsMenu()
		{
			Close();
			optionsMenu.gameObject.SetActive(true);
		}


		void ToggleMenuVisibility()
		{
			if (MenuIsOpen())
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		void Close()
		{
			menuPopup.gameObject.SetActive(false);
		}

		void CloseAll()
		{
			Close();
			libraryMenu.Close();
			saveMenu.Close();
		}

		void Quit()
		{
			CloseAll();

			ChipEditor chipEditor = chipCreationManager.ActiveEditChipEditor;

			if (chipEditor.HasUnsavedChanges())
			{
				string quitMessage = "Are you sure you want to quit? The current chip has unsaved changes which will be lost.";
				confirmationPopup.Open(quitMessage, "CANCEL", "QUIT", null, ConfirmQuit);
			}
			else
			{
				ConfirmQuit();
			}
		}

		void ConfirmQuit()
		{
			ProjectManager.QuitToMainMenu();
		}

		void Open()
		{
			menuPopup.gameObject.SetActive(true);

			bool inEditMode = chipCreationManager.ActiveEditChipEditor == chipCreationManager.ActiveViewChipEditor;
			saveButton.SetInteractable(inEditMode);
			libraryButton.SetInteractable(inEditMode);
			newButton.SetInteractable(inEditMode);
		}

		bool MenuIsOpen() => menuPopup.gameObject.activeSelf;

	}
}