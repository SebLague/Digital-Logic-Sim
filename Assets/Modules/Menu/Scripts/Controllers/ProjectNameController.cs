using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipCreation;
using System.Linq;

namespace DLS.MainMenu
{
	public class ProjectNameController : MonoBehaviour
	{
		public event System.Action<bool, string> ProjectNameChanged;
		public string ProjectName => inputField.text;

		[SerializeField] TMPro.TMP_InputField inputField;
		[SerializeField] TMPro.TMP_Text errorMessage;

		HashSet<string> allExistingProjectNames;

		void Awake()
		{
			inputField.onValueChanged.AddListener(OnProjectNameChanged);
		}

		public void ResetController()
		{
			allExistingProjectNames = new(ProjectSettingsLoader.LoadAllProjectSettings().Select(p => p.ProjectName), System.StringComparer.OrdinalIgnoreCase);

			inputField.Select();
			inputField.SetTextWithoutNotify("");
			OnProjectNameChanged("");
		}

		void OnProjectNameChanged(string projectName)
		{
			bool emptyName = string.IsNullOrWhiteSpace(projectName);
			bool duplicateName = allExistingProjectNames.Contains(projectName);
			bool unsupportedFileName = !NameValidationHelper.ValidFileName(projectName);

			bool validName = !(emptyName || duplicateName || unsupportedFileName);

			errorMessage.gameObject.SetActive(!validName);
			if (emptyName)
			{
				errorMessage.text = "Please enter a name for your project.";
			}
			else if (duplicateName)
			{
				errorMessage.text = "A project with this name already exists.";
			}
			else if (unsupportedFileName)
			{
				errorMessage.text = "This name is reserved.";
			}

			ProjectNameChanged?.Invoke(validName, projectName);
		}
	}
}