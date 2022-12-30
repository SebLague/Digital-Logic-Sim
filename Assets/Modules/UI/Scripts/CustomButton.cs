using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DLS.ChipCreation
{
	public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, ISelectHandler
	{
		public event System.Action ButtonClicked;
		public event System.Action ButtonRightClicked;
		public event System.Action ButtonPressedDown;
		public event System.Action Selected;
		public Button Button => _button ??= GetComponent<Button>();

		[Header("Settings")]
		[SerializeField] string text = "Press Me";
		[SerializeField] Color textNormalCol = Color.white;
		[SerializeField] Color textHighlightCol = Color.white;
		[SerializeField] Color disabledTextCol = Color.white;

		[Header("References")]
		[SerializeField] TMPro.TMP_Text textDisplay;

		RectTransform rectTransform;
		Button _button;
		ColorBlock originalCols;

		public delegate void Function();

		void Awake()
		{
			rectTransform = GetComponent<RectTransform>();
			Button.onClick.AddListener(OnButtonClicked);
			originalCols = Button.colors;

		}

		void OnEnable()
		{
			textDisplay.color = Button.interactable ? textNormalCol : disabledTextCol;
		}

		public void SetHighlightColour(Color col)
		{
			var cols = Button.colors;
			cols.highlightedColor = col;
			Button.colors = cols;
		}

		public void SetNormalColour(Color col)
		{
			var cols = Button.colors;
			cols.normalColor = col;
			Button.colors = cols;
		}

		public void ResetColours()
		{
			Button.colors = originalCols;
		}

		public void SetInteractable(bool isInteractable)
		{
			Button.interactable = isInteractable;
			UpdateTextColour(Button.interactable ? textNormalCol : disabledTextCol);
		}

		public void SetButtonText(string text)
		{
			this.text = text;
			textDisplay.text = text;
		}

		public string GetButtonText()
		{
			return text;
		}

		public void ScaleToTextWidth(float padding)
		{
			float width = textDisplay.GetPreferredValues(textDisplay.text).x + padding;
			rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (Button.interactable)
			{
				if (eventData.button == PointerEventData.InputButton.Right)
				{
					ButtonRightClicked?.Invoke();
				}
				ButtonPressedDown?.Invoke();
			}
		}

		public void RegisterFakeButtonPress()
		{
			ButtonPressedDown?.Invoke();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (Button.interactable)
			{
				UpdateTextColour(textHighlightCol);
			}
		}

		public void OnSelect(BaseEventData e)
		{
			Selected?.Invoke();
		}


		public void OnPointerExit(PointerEventData eventData)
		{
			if (Button.interactable)
			{
				UpdateTextColour(textNormalCol);
			}
		}


		void OnButtonClicked()
		{
			ButtonClicked?.Invoke();
		}

		protected virtual void UpdateTextColour(Color col)
		{
			textDisplay.color = col;
		}

		void OnValidate()
		{
			if (Application.isEditor)
			{
				textDisplay ??= GetComponentInChildren<TMPro.TMP_Text>();
				if (textDisplay != null)
				{
					UpdateTextColour((Button.interactable) ? textNormalCol : disabledTextCol);
					textDisplay.text = text;
				}
			}
		}
	}
}