using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SebUI
{
	[ExecuteInEditMode]
	public class ValueWheel : MonoBehaviour
	{

		public event System.Action<int> onValueChanged;
		public string[] values;
		public int activeValueIndex;

		[Header("References")]
		[SerializeField] Button leftButton;
		[SerializeField] Button rightButton;
		[SerializeField] TMP_Text valueLabel;
		[SerializeField] RectTransform valueBox;
		int widthOld;

		void Start()
		{
			if (Application.isPlaying)
			{
				leftButton.onClick.AddListener(() => MoveIndex(-1));
				rightButton.onClick.AddListener(() => MoveIndex(+1));
				UpdateDisplayValue();
			}
		}

		void Update()
		{
			if (!Application.isPlaying)
			{
				EditorOnlyUpdate();
			}
		}

		public void SetActiveIndex(int newIndex, bool notify = true)
		{
			newIndex = Mathf.Clamp(newIndex, 0, values.Length - 1);

			if (activeValueIndex != newIndex)
			{
				activeValueIndex = newIndex;
				UpdateDisplayValue();
				if (notify)
				{
					onValueChanged?.Invoke(activeValueIndex);
				}
			}
		}

		void MoveIndex(int direction)
		{
			int newIndex = activeValueIndex + direction;
			newIndex = Mathf.Clamp(newIndex, 0, values.Length - 1);

			SetActiveIndex(newIndex);
		}

		public void SetPossibleValues(string[] values, int activeIndex)
		{
			this.values = values;
			this.activeValueIndex = activeIndex;
			UpdateDisplayValue();
		}



		void UpdateDisplayValue()
		{
			leftButton.interactable = activeValueIndex > 0;
			rightButton.interactable = activeValueIndex < values.Length - 1;

			if (values != null && values.Length > 0)
			{
				valueLabel.text = values[activeValueIndex];
			}
		}

		void EditorOnlyUpdate()
		{
			UpdateDisplayValue();
		}

		void OnValidate()
		{
			activeValueIndex = Mathf.Clamp(activeValueIndex, 0, values.Length - 1);
		}

	}
}