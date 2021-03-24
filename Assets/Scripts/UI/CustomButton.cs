using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button, IPointerDownHandler {
	public event System.Action onPointerDown;

	public override void OnPointerDown (PointerEventData eventData) {
		base.OnPointerDown (eventData);
		onPointerDown?.Invoke ();
	}
}