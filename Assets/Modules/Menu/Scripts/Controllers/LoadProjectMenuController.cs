using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipCreation;

namespace DLS.MainMenu
{
	public class LoadProjectMenuController : MonoBehaviour
	{
		[SerializeField] Color highlightCol;

		[Header("References")]
		[SerializeField] MainMenuController mainMenu;
		[SerializeField] Menu loadProjectMenu;
		[SerializeField] CustomButton renameButton;
		[SerializeField] CustomButton deleteButton;
		[SerializeField] CustomButton copyButton;
		[SerializeField] CustomButton openButton;
		[SerializeField] RectTransform projectListHolder;
		[SerializeField] CustomButton projectNamePrefab;
		[SerializeField] GameObject deleteConfirmMenu;
		[SerializeField] CustomButton cancelDelete;
		[SerializeField] CustomButton confirmDelete;
		[SerializeField] GameObject renamePopupMenu;
		[SerializeField] ProjectNameController projectNameController;
		[SerializeField] CustomButton cancelRenameButton;
		[SerializeField] CustomButton confirmRenameButton;

		ProjectSettings[] allProjectSettings;

		CustomButton selectedButton;
		string selectedProjectName;
		bool renameState_isCopying;
		Dictionary<string, CustomButton> buttonLookup;

		void Awake()
		{
			RefreshProjectList();
			loadProjectMenu.MenuOpened += ResetMenu;

			// Add button listeners
			openButton.ButtonClicked += OpenSelectedProject;
			deleteButton.ButtonClicked += () => deleteConfirmMenu.SetActive(true);
			confirmDelete.ButtonClicked += ConfirmDelete;
			cancelDelete.ButtonClicked += () => deleteConfirmMenu.SetActive(false);
			renameButton.ButtonClicked += () => OpenRenamePopup(false);
			copyButton.ButtonClicked += () => OpenRenamePopup(true);
			cancelRenameButton.ButtonClicked += () => renamePopupMenu.SetActive(false);
			confirmRenameButton.ButtonClicked += ConfirmRename;
		}

		void RefreshProjectList()
		{
			allProjectSettings = ProjectSettingsLoader.LoadAllProjectSettings();

			// Delete all old project buttons
			selectedButton = null;
			selectedProjectName = string.Empty;
			for (int i = projectListHolder.childCount - 1; i >= 0; i--)
			{
				Destroy(projectListHolder.GetChild(i).gameObject);
			}

			buttonLookup = new Dictionary<string, CustomButton>(System.StringComparer.OrdinalIgnoreCase);

			// Add projects to scroll list
			for (int i = 0; i < allProjectSettings.Length; i++)
			{
				string projectName = allProjectSettings[i].ProjectName;
				CustomButton projectButton = Instantiate(projectNamePrefab, parent: projectListHolder);
				projectButton.SetButtonText(projectName);

				projectButton.ButtonPressedDown += () => Select(projectButton, projectName);
				buttonLookup.Add(projectName, projectButton);
			}
		}

		void Select(CustomButton button, string projectName)
		{
			selectedButton?.ResetColours();
			button.SetNormalColour(highlightCol);
			button.SetHighlightColour(highlightCol);
			selectedProjectName = projectName;
			selectedButton = button;
			SetButtonsInteractable(true);

		}

		void OpenSelectedProject()
		{
			ProjectManager.SetStartupProject(selectedProjectName);
			mainMenu.Play();
		}

		void ResetMenu()
		{
			selectedButton?.ResetColours();
			SetButtonsInteractable(false);
			selectedProjectName = "";
		}

		void ConfirmDelete()
		{
			ProjectSettingsLoader.DeleteProject(selectedProjectName);
			Destroy(selectedButton.gameObject);
			deleteConfirmMenu.SetActive(false);
			selectedButton = null;
			selectedProjectName = string.Empty;
			SetButtonsInteractable(false);
		}

		void OpenRenamePopup(bool isCreatingCopy)
		{
			renamePopupMenu.SetActive(true);
			projectNameController.ResetController();
			confirmRenameButton.SetButtonText(isCreatingCopy ? "CREATE COPY" : "RENAME");
			renameState_isCopying = isCreatingCopy;
		}

		void ConfirmRename()
		{
			string nameOld = selectedProjectName;
			string nameNew = projectNameController.ProjectName;

			if (renameState_isCopying)
			{
				ProjectSettingsLoader.CreateCopyOfProject(nameOld, nameNew);
			}
			else
			{
				ProjectSettingsLoader.RenameProject(nameOld, nameNew);
			}

			RefreshProjectList();
			renamePopupMenu.SetActive(false);
			TrySelectButtonByName(nameNew);
		}

		void SetButtonsInteractable(bool canInteract)
		{
			renameButton.SetInteractable(canInteract);
			copyButton.SetInteractable(canInteract);
			deleteButton.SetInteractable(canInteract);
			openButton.SetInteractable(canInteract);
		}

		void TrySelectButtonByName(string name)
		{
			if (buttonLookup.ContainsKey(name))
			{
				buttonLookup[name].RegisterFakeButtonPress();
			}
		}
	}
}