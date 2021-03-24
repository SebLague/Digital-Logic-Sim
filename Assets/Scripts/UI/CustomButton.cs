using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler {
	public event System.Action onPointerDown;
	public event System.Action onPointerEnter;
	public event System.Action onPointerExit;

	public override void OnPointerDown (PointerEventData eventData) {
		base.OnPointerDown (eventData);
		onPointerDown?.Invoke ();
	}

	public override void OnPointerEnter (PointerEventData eventData) {
		base.OnPointerEnter (eventData);
		onPointerEnter?.Invoke ();
	}

	public override void OnPointerExit (PointerEventData eventData) {
		base.OnPointerExit (eventData);
		onPointerExit?.Invoke ();
	}
}