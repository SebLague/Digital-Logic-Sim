using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button, IPointerDownHandler {
	public event System.Action onPointerDown;
	public List<System.Action> events = new List<System.Action>();

	public override void OnPointerDown (PointerEventData eventData) {
		base.OnPointerDown (eventData);
		onPointerDown?.Invoke ();
	}

	public void AddListener(System.Action action) {
		onPointerDown += action;
		events.Add(action);
	}

	public void ClearEvents() {
		foreach (System.Action a in events) {
			onPointerDown -= a;
		}
		events.Clear();
	}
}