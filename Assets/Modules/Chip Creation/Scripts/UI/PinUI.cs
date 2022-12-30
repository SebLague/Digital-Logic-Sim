using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DLS.ChipCreation
{
	public class PinUI : MonoBehaviour
	{
		public event System.Action<string> NameChanged;
		public event System.Action DeletePressed;

		[SerializeField] RectTransform pinEditPopup;
		[SerializeField] float padding;
		[SerializeField] TMPro.TMP_InputField pinNameField;
		[SerializeField] UnityEngine.UI.CanvasScaler scaler;
		[SerializeField] UnityEngine.UI.Button deleteButton;
		[SerializeField] RectTransform rect;
		Camera cam;


		void Start()
		{
			cam = Camera.main;
			pinEditPopup.gameObject.SetActive(false);
			pinNameField.onValueChanged.AddListener(OnNameChanged);
			deleteButton.onClick.AddListener(OnDeleteButtonPressed);
		}

		public bool MouseIsOverWindow()
		{
			return RectTransformUtility.RectangleContainsScreenPoint(rect, MouseHelper.GetMouseScreenPosition(), cam);
		}

		public void Show(Vector2 position, bool isInput, string text)
		{
			pinEditPopup.gameObject.SetActive(true);
			SetPosition(position, isInput);
			pinNameField.SetTextWithoutNotify(text);
			pinNameField.Select();
		}

		public void SetPosition(Vector2 position, bool isInput)
		{
			float offsetX = (pinEditPopup.sizeDelta.x / 2 + padding) * (isInput ? 1 : -1);
			Vector2 screenPos = CalcPos(position) + Vector2.right * offsetX;

			pinEditPopup.localPosition = screenPos;
		}

		Vector2 CalcPos(Vector2 worldPos)
		{
			Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
			return UIHelper.CalcCanvasLocalPos(screenPos, scaler.referenceResolution.x, scaler.referenceResolution.y);
		}

		public void Hide()
		{
			pinEditPopup.gameObject.SetActive(false);
		}

		void OnNameChanged(string newName)
		{
			NameChanged?.Invoke(newName);
		}

		void OnDeleteButtonPressed()
		{
			DeletePressed?.Invoke();
			Hide();
		}
	}
}
