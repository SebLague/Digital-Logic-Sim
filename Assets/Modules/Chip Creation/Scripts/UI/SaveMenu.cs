using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	public class SaveMenu : MonoBehaviour
	{
		enum ChipNameState { ValidNewName, ValidSameName, Empty, AlreadyExists, Reserved, BuiltinName }

		[SerializeField] ProjectManager chipCreationManager;
		[SerializeField] TMP_InputField nameInputField;
		[SerializeField] CustomButton cancelButton;
		[SerializeField] CustomButton saveCopyButton;
		[SerializeField] CustomButton saveButton;
		[SerializeField] ColourWindow colourWindow;
		[SerializeField] Image colourPreview;
		[SerializeField] TMP_Text colourPreviewText;
		[SerializeField] Button randomizeColourButton;
		[SerializeField] TMP_Text nameErrorMessage;
		[SerializeField] TMP_Text editWarningMessage;

		ChipEditor chipEditor;
		bool isNewChip;
		ChipDescription lastSavedDescription;
		ChipDescription descriptionToSave;

		void Awake()
		{
			// Add Listeners
			cancelButton.ButtonClicked += Close;
			saveButton.ButtonClicked += ConfirmSave;
			saveCopyButton.ButtonClicked += ConfirmSaveAsCopy;
			nameInputField.onValueChanged.AddListener(OnNameChanged);
			randomizeColourButton.onClick.AddListener(RandomizeColour);
			colourWindow.ColourUpdated += OnColourChanged;

		}

		public bool IsOpen() => gameObject.activeSelf;

		public void Open(ChipEditor chipEditor)
		{
			if (!IsOpen())
			{
				gameObject.SetActive(true);

				this.chipEditor = chipEditor;
				descriptionToSave = ChipDescriptionCreator.CreateChipDescription(chipEditor);
				lastSavedDescription = chipEditor.LastSavedDescription;
				isNewChip = !ChipDescriptionLoader.HasLoaded(lastSavedDescription.Name);

				// Setup name input field
				nameInputField.text = chipEditor.LastSavedDescription.Name;
				nameInputField.Select();

				nameInputField.MoveTextEnd(false);

				// Setup colour
				if (ChipDescriptionLoader.HasLoaded(descriptionToSave.Name) && ColorUtility.TryParseHtmlString(descriptionToSave.Colour, out Color prevCol))
				{
					colourWindow.SetInitialColour(prevCol, true);
				}
				else
				{
					RandomizeColour();
				}

				UpdateSaveValidity();
				SetWarningMessage();
				saveCopyButton.gameObject.SetActive(!isNewChip);

			}
		}

		void SetWarningMessage()
		{
			string[] chipsUsingCurrentChip = ChipDescriptionHelper.GetParentChipNames(chipEditor.LastSavedDescription.Name);
			int numUses = chipsUsingCurrentChip.Length;
			editWarningMessage.gameObject.SetActive(numUses > 0);

			string message = "<b>Warning:</b> saving changes to this chip will affect all instances where it has been used (unless saved as a copy). ";
			message += CreateChipInUseWarningMessage(chipsUsingCurrentChip);
			editWarningMessage.text = message;
		}

		public static string CreateChipInUseWarningMessage(string[] chipsUsingCurrentChip)
		{
			int numUses = chipsUsingCurrentChip.Length;
			if (numUses == 1)
			{
				return $"This chip is currently used in {FormatChipName(0)}.";
			}
			else if (numUses == 2)
			{
				return $"This chip is currently used in {FormatChipName(0)} and {FormatChipName(1)}.";
			}
			else if (numUses > 2)
			{
				return $"This chip is currently used in {FormatChipName(0)} and {numUses - 1} others.";
			}
			return "";

			string FormatChipName(int index)
			{
				return $"\"{chipsUsingCurrentChip[index]}\"";
			}
		}

		void UpdateSaveValidity()
		{
			ChipNameState nameState = ValidateChipName();
			saveButton.SetInteractable(nameState is ChipNameState.ValidNewName or ChipNameState.ValidSameName);
			saveCopyButton.SetInteractable(nameState is ChipNameState.ValidNewName);
			switch (nameState)
			{
				case ChipNameState.AlreadyExists:
					nameErrorMessage.text = "Another chip with this name already exists.";
					break;
				case ChipNameState.Reserved or ChipNameState.BuiltinName:
					nameErrorMessage.text = "This name is reserved. Please choose something else.";
					break;
				default:
					nameErrorMessage.text = "";
					break;
			}
		}

		void OnNameChanged(string newName)
		{
			descriptionToSave.Name = newName;
			UpdateSaveValidity();
		}

		ChipNameState ValidateChipName()
		{
			string saveName = descriptionToSave.Name;
			bool sameNameAsBefore = string.Equals(saveName, chipEditor.LastSavedDescription.Name, System.StringComparison.OrdinalIgnoreCase);

			if (BuiltinChipNames.IsBuiltinName(saveName))
			{
				return ChipNameState.BuiltinName;
			}
			if (!sameNameAsBefore)
			{
				if (ChipDescriptionLoader.HasLoaded(saveName))
				{
					return ChipNameState.AlreadyExists;
				}
			}
			if (string.IsNullOrWhiteSpace(saveName))
			{
				return ChipNameState.Empty;
			}
			if (!NameValidationHelper.ValidFileName(saveName))
			{
				return ChipNameState.Reserved;
			}
			return sameNameAsBefore ? ChipNameState.ValidSameName : ChipNameState.ValidNewName;
		}


		public void Close()
		{
			if (IsOpen())
			{
				gameObject.SetActive(false);
			}
		}

		void ConfirmSave()
		{
			bool hasRenamed = !isNewChip && descriptionToSave.Name != lastSavedDescription.Name;
			chipCreationManager.SaveChip(isNewChip, hasRenamed, descriptionToSave);
			Close();
		}

		void ConfirmSaveAsCopy()
		{
			chipCreationManager.SaveChip(isFirstTimeSaving: true, isRenamingExistingChip: false, descriptionToSave);
			Close();
		}


		void OnColourChanged(Color col)
		{
			colourPreview.color = col;
			colourPreviewText.color = ColourHelper.TextBlackOrWhite(col);
			descriptionToSave.Colour = $"#{ColorUtility.ToHtmlStringRGB(col)}";
		}

		void RandomizeColour()
		{
			colourWindow.SetInitialColour(ColourHelper.GenerateRandomChipColour(), true);
		}
	}
}