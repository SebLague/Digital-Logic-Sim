using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation.UI
{
	public class ContextMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public event System.Action MenuClosed;

		[SerializeField] CustomButton buttonPrefab;
		[SerializeField] RectTransform dividerPrefab;
		[SerializeField] GameObject headerHolder;
		[SerializeField] TMPro.TMP_Text headerText;
		RectTransform rectTransform;
		bool isClosed;
		bool mouseIsOver;


		void Awake()
		{
			rectTransform = GetComponent<RectTransform>();
			headerHolder.SetActive(false);
		}

		void Update()
		{
			Mouse mouse = Mouse.current;
			if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
			{
				if (!mouseIsOver)
				{
					Close();
				}
			}

			// Close menu on escape or backspace pressed
			if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
			{
				Close();
			}
		}

		public void SetTitle(string text)
		{
			headerText.text = text;
			headerHolder.gameObject.SetActive(!string.IsNullOrEmpty(text));
		}

		public void AddDivider()
		{
			Instantiate(dividerPrefab, parent: transform);
		}

		public void AddButton(string buttonText, System.Action callback)
		{
			CustomButton button = Instantiate(buttonPrefab, parent: transform);
			button.ButtonClicked += callback;
			button.ButtonClicked += Close;

			button.SetButtonText(buttonText);
		}

		public void SetPosition(Vector2 screenPosition)
		{
			rectTransform.localPosition = UIHelper.CalcCanvasLocalPos(screenPosition);
		}

		public void Close()
		{
			if (!isClosed)
			{
				isClosed = true;
				MenuClosed?.Invoke();
				Destroy(gameObject);
			}
		}

		public void OnPointerEnter(PointerEventData e)
		{
			mouseIsOver = true;
		}

		public void OnPointerExit(PointerEventData e)
		{
			mouseIsOver = false;
		}

	}
}