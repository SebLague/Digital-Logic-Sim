using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebUI;

namespace DLS.ChipCreation.UI
{
	public class EditorOptionsMenu : MonoBehaviour
	{
		[SerializeField] ProjectManager chipCreationManager;
		[SerializeField] ValueWheel mainPinDisplayOptions;
		[SerializeField] ValueWheel subPinDisplayOptions;
		[SerializeField] ValueWheel cursorGuideDisplayOptions;
		[SerializeField] CustomButton doneButton;

		void Awake()
		{
			mainPinDisplayOptions.onValueChanged += (v) => UpdatePinDisplayOptions();
			subPinDisplayOptions.onValueChanged += (v) => UpdatePinDisplayOptions();
			cursorGuideDisplayOptions.onValueChanged += (v) => UpdatePinDisplayOptions();
			doneButton.ButtonClicked += Close;
		}

		void OnEnable()
		{
			DisplayOptions options = chipCreationManager.ProjectSettings.DisplayOptions;
			mainPinDisplayOptions.SetActiveIndex((int)options.MainChipPinNameDisplayMode, false);
			subPinDisplayOptions.SetActiveIndex((int)options.SubChipPinNameDisplayMode, false);
			cursorGuideDisplayOptions.SetActiveIndex((int)options.ShowCursorGuide, false);
		}

		void UpdatePinDisplayOptions()
		{
			Debug.Log("Updated");
			DisplayOptions options = new DisplayOptions()
			{
				MainChipPinNameDisplayMode = (DisplayOptions.PinNameDisplayMode)mainPinDisplayOptions.activeValueIndex,
				SubChipPinNameDisplayMode = (DisplayOptions.PinNameDisplayMode)subPinDisplayOptions.activeValueIndex,
				ShowCursorGuide = (DisplayOptions.ToggleState)cursorGuideDisplayOptions.activeValueIndex
			};
			chipCreationManager.UpdateDisplayOptions(options);
		}

		void Close()
		{
			gameObject.SetActive(false);
		}
	}
}