using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DLS.ChipCreation;
using UnityEngine.UI;

namespace DLS.MainMenu
{
	public class NewProjectMenuController : MonoBehaviour
	{
		[SerializeField] MainMenuController mainMenu;
		[SerializeField] Menu newProjectMenu;
		[SerializeField] CustomButton createButton;
		[SerializeField] ProjectNameController projectNameController;

		void Awake()
		{
			newProjectMenu.MenuOpened += ResetMenu;
			createButton.ButtonClicked += Create;
			projectNameController.ProjectNameChanged += OnProjectNameChanged;
		}

		void Create()
		{
			string newProjectName = projectNameController.ProjectName;
			ProjectSettingsLoader.CreateProject(newProjectName);
			ProjectManager.SetStartupProject(newProjectName);
			mainMenu.Play();
		}

		void ResetMenu()
		{
			projectNameController.ResetController();
		}

		void OnProjectNameChanged(bool isValid, string projectName)
		{
			createButton.SetInteractable(isValid);
		}
	}
}