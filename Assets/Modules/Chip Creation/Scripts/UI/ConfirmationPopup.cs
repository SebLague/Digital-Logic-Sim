using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DLS.ChipCreation.UI
{
	public class ConfirmationPopup : MonoBehaviour
	{
		System.Action cancelledCallback;
		System.Action confirmedCallback;

		[SerializeField] CustomButton cancelButton;
		[SerializeField] CustomButton confirmButton;
		[SerializeField] TMPro.TMP_Text messageUI;

		void Awake()
		{
			cancelButton.ButtonClicked += OnCancel;
			confirmButton.ButtonClicked += OnConfirm;
		}

		public void Open(string message, string cancelText, string confirmText, System.Action cancelCallback, System.Action confirmCallback)
		{
			messageUI.text = message;
			cancelButton.SetButtonText(cancelText);
			confirmButton.SetButtonText(confirmText);
			cancelledCallback = cancelCallback;
			confirmedCallback = confirmCallback;
			gameObject.SetActive(true);
		}

		public void Close()
		{
			gameObject.SetActive(false);
		}

		void OnCancel()
		{
			Close();
			cancelledCallback?.Invoke();
		}

		void OnConfirm()
		{
			Close();
			confirmedCallback?.Invoke();
		}


	}
}