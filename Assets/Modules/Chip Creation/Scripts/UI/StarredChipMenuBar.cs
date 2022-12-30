using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using UnityEngine.UI;

namespace DLS.ChipCreation.UI
{
	public class StarredChipMenuBar : MonoBehaviour
	{

		[SerializeField] ProjectManager chipCreationManager;
		[SerializeField] CustomButton buttonPrefab;
		[SerializeField] HorizontalLayoutGroup buttonHolder;
		[SerializeField] ChipLibraryMenu libraryMenu;

		List<CustomButton> allButtons;
		List<ChipDescription> chipDescriptions;

		public void SetUp()
		{
			allButtons = new();
			chipDescriptions = new();
			chipCreationManager.ViewedChipChanged += RefreshButtonInteractivity;
			chipCreationManager.ChipSaved += RefreshButtons;
			chipCreationManager.SavedChipDeleted += RefreshButtons;
			libraryMenu.ChipStarStatusUpdated += RefreshButtons;
			RefreshButtons();
		}

		void RefreshButtons()
		{
			for (int i = 0; i < allButtons.Count; i++)
			{
				if (allButtons[i] != null)
				{
					Destroy(allButtons[i].gameObject);
				}
			}
			allButtons.Clear();
			chipDescriptions.Clear();

			var starredChipNames = chipCreationManager.ProjectSettings.GetStarredChipNames();
			foreach (string chipName in starredChipNames)
			{
				var chipDescription = ChipDescriptionLoader.GetChipDescription(chipName);

				CustomButton button = CreateButton(chipName);
				button.ButtonClicked += () => OnButtonPressed(chipDescription);

				allButtons.Add(button);
				chipDescriptions.Add(chipDescription);
			}

			RefreshButtonInteractivity(chipCreationManager.ActiveEditChipEditor);
		}

		void RefreshButtonInteractivity(ChipEditor viewedEditor)
		{
			for (int i = 0; i < allButtons.Count; i++)
			{
				allButtons[i].SetInteractable(CanInteract(chipDescriptions[i]));
			}
		}

		bool CanInteract(ChipDescription chip)
		{
			if (!chipCreationManager.ActiveViewChipEditor.CanEdit)
			{
				return false;
			}
			// Cannot add a chip if it's the same chip (or it contains a subchip) that's currently being edited
			IList<string> allNamesInChip = ChipDescriptionHelper.GetAllSubChipNames(chip.Name, includeParentName: true);
			return !allNamesInChip.Contains(chipCreationManager.ActiveEditChipEditor.LastSavedDescription.Name);
		}

		CustomButton CreateButton(string name)
		{
			CustomButton button = Instantiate(buttonPrefab, parent: buttonHolder.transform);
			button.SetButtonText(name);
			button.ScaleToTextWidth(padding: 25f);

			return button;
		}

		void OnButtonPressed(ChipDescription chipDescription)
		{
			chipCreationManager.ActiveEditChipEditor.ChipPlacer.StartPlacingChip(chipDescription);
		}
	}
}
