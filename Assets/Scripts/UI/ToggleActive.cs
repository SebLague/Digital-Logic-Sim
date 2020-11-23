using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleActive : MonoBehaviour {
	public void Toggle () {
		gameObject.SetActive (!gameObject.activeSelf);
	}
}