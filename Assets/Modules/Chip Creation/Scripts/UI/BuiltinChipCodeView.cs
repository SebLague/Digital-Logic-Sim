using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
namespace DLS.ChipCreation.UI
{
	public class BuiltinChipCodeView : MonoBehaviour
	{
		[SerializeField] TextAsset codeFile_And;
		[SerializeField] TextAsset codeFile_Not;
		[SerializeField] TextAsset codeFile_Clock;
		[SerializeField] TextAsset codeFile_SevenSegDisplay;

		[SerializeField] float backgroundPadding;
		[SerializeField] float outlinePadding;
		[SerializeField] GameObject holder;
		[SerializeField] TextAsset codeFile;
		[SerializeField] TMPro.TMP_Text codeTextDisplay;
		[SerializeField] RectTransform background;
		[SerializeField] RectTransform outline;
		[SerializeField] RectTransform builtinChipLabel;


		void Start()
		{
			holder.SetActive(false);

		}

		public void OnViewChanged(string chipName)
		{
			holder.SetActive(false);
			TextAsset codeFile = GetFileFromName(chipName);
			if (codeFile != null)
			{
				Set(codeFile);
			}

			TextAsset GetFileFromName(string chipName)
			{
				switch (chipName)
				{
					case BuiltinChipNames.AndChip: return codeFile_And;
					case BuiltinChipNames.NotChip: return codeFile_Not;
					case BuiltinChipNames.ClockName: return codeFile_Clock;
					case BuiltinChipNames.SevenSegmentDisplayName: return codeFile_SevenSegDisplay;
					default: return null;
				}
			}
		}

		void Set(TextAsset file)
		{
			holder.SetActive(true);
			codeTextDisplay.text = file.text;
			Vector2 size = codeTextDisplay.GetPreferredValues();
			codeTextDisplay.GetComponent<RectTransform>().sizeDelta = size;
			background.sizeDelta = size + Vector2.one * backgroundPadding;
			outline.sizeDelta = size + Vector2.one * (backgroundPadding + outlinePadding);
			builtinChipLabel.localPosition = outline.localPosition + Vector3.up * (outline.sizeDelta.y / 2 + 30);
		}
	}
}