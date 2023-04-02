using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Honeti;

namespace DLS.ChipCreation.UI
{
	public class MenuPopup : MonoBehaviour
	{

		[SerializeField] Button menuButton;
		[SerializeField] RectTransform menuPopup;
		[SerializeField] ProjectManager projectManager;
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

			if (projectManager.ActiveViewChipEditor.CanEdit)
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

					if (keyboard.spaceKey.wasPressedThisFrame)
					{
						ToggleMouseGuide();
					}
				}
			}

			// Close all menus shortcut
			if (keyboard.escapeKey.wasPressedThisFrame)
			{
				CloseAll();
			}
		}

		void ToggleMouseGuide()
		{
			var options = projectManager.ProjectSettings.DisplayOptions;
			options.ShowCursorGuide = options.ShowCursorGuide is DisplayOptions.ToggleState.On ? DisplayOptions.ToggleState.Off : DisplayOptions.ToggleState.On;
			projectManager.UpdateDisplayOptions(options);
		}

		void CreateNewChip()
		{
			CloseAll();

			ChipEditor chipEditor = projectManager.ActiveEditChipEditor;

			if (chipEditor.HasUnsavedChanges())
			{
				string message = I18N.instance.getValue("^create_new_warning");
				confirmationPopup.Open(message, I18N.instance.getValue("^cancel"), I18N.instance.getValue("^create_new"), null, ConfirmCreateNewChip);
			}
			else
			{
				ConfirmCreateNewChip();
			}

		}

		void ConfirmCreateNewChip()
		{
			projectManager.OpenNewChipEditor();
		}

		void OpenSaveMenu()
		{
			Close();
			saveMenu.Open(projectManager.ActiveEditChipEditor);
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

			ChipEditor chipEditor = projectManager.ActiveEditChipEditor;

			if (chipEditor.HasUnsavedChanges())
			{
				string quitMessage = I18N.instance.getValue("^quit_warning");
				confirmationPopup.Open(quitMessage, I18N.instance.getValue("^cancel"), I18N.instance.getValue("^quit"), null, ConfirmQuit);
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

			bool inEditMode = projectManager.ActiveEditChipEditor == projectManager.ActiveViewChipEditor;
			saveButton.SetInteractable(inEditMode);
			libraryButton.SetInteractable(inEditMode);
			newButton.SetInteractable(inEditMode);
		}

		bool MenuIsOpen() => menuPopup.gameObject.activeSelf;

	}
}