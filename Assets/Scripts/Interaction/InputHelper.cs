using UnityEngine;

public static class InputHelper {
	static Camera _mainCamera;

	// Constructor
	static Camera MainCamera {
		get {
			if (_mainCamera == null) {
				_mainCamera = Camera.main;
			}
			return _mainCamera;
		}
	}

	public static Vector2 MouseWorldPos {
		get {
			return MainCamera.ScreenToWorldPoint (Input.mousePosition);
		}
	}

	public static bool MouseOverUIObject () {
		return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject ();
	}

	public static GameObject GetObjectUnderMouse2D (LayerMask mask) {
		Vector2 mouse = MouseWorldPos;
		var hit = Physics2D.GetRayIntersection (new Ray (new Vector3 (mouse.x, mouse.y, -100), Vector3.forward), float.MaxValue, mask);
		if (hit.collider) {
			return hit.collider.gameObject;
		}
		return null;
	}

	public static bool AnyOfTheseKeysDown (params KeyCode[] keys) {
		for (int i = 0; i < keys.Length; i++) {
			if (Input.GetKeyDown (keys[i])) {
				return true;
			}
		}
		return false;
	}

	public static bool AnyOfTheseKeysHeld (params KeyCode[] keys) {
		for (int i = 0; i < keys.Length; i++) {
			if (Input.GetKey (keys[i])) {
				return true;
			}
		}
		return false;
	}

}