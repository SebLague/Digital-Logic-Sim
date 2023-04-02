using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DLS.ChipData;
using Honeti;

namespace DLS.ChipCreation.UI
{
	public class ChipLibraryMenu : MonoBehaviour
	{
		public event System.Action ChipStarStatusUpdated;

		[SerializeField] Color highlightCol;

		[Header("References")]
		[SerializeField] ProjectManager projectManager;
		[SerializeField] RectTransform headingPrefab;
		[SerializeField] CustomButton chipButtonPrefab;
		[SerializeField] Button closeButton;
		[SerializeField] CustomButton addButton;
		[SerializeField] CustomButton editButton;
		[SerializeField] CustomButton starButton;
		[SerializeField] CustomButton deleteButton;
		[SerializeField] RectTransform contentHolder;
		[SerializeField] ConfirmationPopup confirmationPopup;

		bool hasSelectedChip => selectedButton is not null;
		ChipDescription selectedChipDescription;
		CustomButton selectedButton;

		void Start()
		{
			addButton.ButtonClicked += OnAddChipPressed;
			closeButton.onClick.AddListener(Close);
			editButton.ButtonClicked += OnEditPressed;
			starButton.ButtonClicked += OnToggleStarred;
			deleteButton.ButtonClicked += OnDeletePressed;
		}

		public bool IsOpen() => gameObject.activeSelf;

		public void Open()
		{
			if (!IsOpen())
			{
				gameObject.SetActive(true);
				ResetUI();

				AddHeading(I18N.instance.getValue("^builtin"));
				foreach (var chip in ChipDescriptionLoader.BuiltinChips)
				{
					AddButton(chip);
				}

				AddHeading(I18N.instance.getValue("^custom"));
				var allCustomChipNames = projectManager.ProjectSettings.GetAllCreatedChipNames();
				foreach (var chipName in allCustomChipNames)
				{
					AddButton(ChipDescriptionLoader.GetChipDescription(chipName));
				}
			}
		}

		public void Close()
		{
			if (IsOpen())
			{
				gameObject.SetActive(false);
			}
		}

		void OnEditPressed()
		{
			if (projectManager.ActiveEditChipEditor.HasUnsavedChanges())
			{
				string message = I18N.instance.getValue("^edit_warning");
				confirmationPopup.Open(message, I18N.instance.getValue("^cancel"), I18N.instance.getValue("^confirm"), null, OnEditConfirmed);
			}
			else
			{
				OnEditConfirmed();
			}

		}

		void OnEditConfirmed()
		{
			projectManager.OpenChipEditor(selectedChipDescription);
			Close();
		}

		void OnAddChipPressed()
		{
			projectManager.ActiveEditChipEditor.ChipPlacer.StartPlacingChip(selectedChipDescription);
			Close();
		}

		void OnToggleStarred()
		{
			bool starred = !projectManager.ProjectSettings.IsStarred(selectedChipDescription.Name);
			projectManager.ProjectSettings.SetStarredState(selectedChipDescription.Name, starred);
			UpdateButtons();
			ChipStarStatusUpdated?.Invoke();
		}

		void OnButtonSelected(CustomButton button, string chipName)
		{
			selectedButton?.ResetColours();
			selectedChipDescription = ChipDescriptionLoader.GetChipDescription(chipName);
			selectedButton = button;
			selectedButton.SetHighlightColour(highlightCol);
			selectedButton.SetNormalColour(highlightCol);

			UpdateButtons();
		}

		void OnDeletePressed()
		{

			string[] parentNames = ChipDescriptionHelper.GetParentChipNames(selectedChipDescription.Name);
			string message = I18N.instance.getValue("^delete_confirm", new string[]{$"\"{selectedChipDescription.Name}\""});
			if (parentNames.Length == 0)
			{
				message += I18N.instance.getValue("^not_used");
			}
			else
			{
				message += SaveMenu.CreateChipInUseWarningMessage(parentNames);
			}
			confirmationPopup.Open(message, I18N.instance.getValue("^cancel"), I18N.instance.getValue("^delete"), null, OnDeleteConfirmed);
		}


		void OnDeleteConfirmed()
		{
			Destroy(selectedButton.gameObject);
			selectedButton = null;
			projectManager.DeleteChip(selectedChipDescription.Name);

			UpdateButtons();
		}


		void UpdateButtons()
		{
			bool isBuiltin = BuiltinChipNames.IsBuiltinName(selectedChipDescription.Name);
			string currentChipName = projectManager.ActiveEditChipEditor.LastSavedDescription.Name;
			bool hasSelectedCurrentlyEditedChip = hasSelectedChip && selectedChipDescription.Name == currentChipName;
			starButton.SetButtonText(I18N.instance.getValue(projectManager.ProjectSettings.IsStarred(selectedChipDescription.Name) ? "^unstar" : "^star"));

			addButton.SetInteractable(hasSelectedChip && !hasSelectedCurrentlyEditedChip);
			starButton.SetInteractable(hasSelectedChip);
			editButton.SetInteractable(!isBuiltin && hasSelectedChip && !hasSelectedCurrentlyEditedChip);
			deleteButton.SetInteractable(!isBuiltin && hasSelectedChip);
		}

		void AddButton(ChipDescription description)
		{
			CustomButton chipButton = Instantiate(chipButtonPrefab, parent: contentHolder);
			chipButton.SetButtonText(description.Name);
			chipButton.ButtonPressedDown += () => OnButtonSelected(chipButton, description.Name);
		}

		void AddHeading(string text)
		{
			var heading = Instantiate(headingPrefab, parent: contentHolder);
			heading.GetComponentInChildren<TMPro.TMP_Text>().text = text;
		}

		void ResetUI()
		{
			selectedButton?.ResetColours();
			DestroyContents();
			UpdateButtons();
		}


		void DestroyContents()
		{
			selectedButton = null;
			for (int i = contentHolder.childCount - 1; i >= 0; i--)
			{
				Destroy(contentHolder.GetChild(i).gameObject);
			}
		}

	}
}