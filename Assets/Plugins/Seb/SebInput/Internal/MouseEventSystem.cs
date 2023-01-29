using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

namespace SebInput.Internal
{
	// Custom mouse event handler to work around some issues I was having with Unity's event system
	// (primarily: PointerEnter not being triggered if an object is instantiated/enabled on top of mouse, and
	// no function to be notified if mouse is released over an object when that object was not the one pressed).
	// NOTE: this only works for UI and 2D colliders (not 3D colliders)

	[DefaultExecutionOrder(-1000)]
	public class MouseEventSystem : MonoBehaviour
	{
		public enum MouseButton { Left, Middle, Right }
		public event System.Action LeftMousePressed;
		public event System.Action RightMousePressed;
		public event System.Action LeftMouseReleased;
		public event System.Action RightMouseReleased;

		[SerializeField] Camera cam;


		List<MouseInteractionListener> listenersWithMouseOver;
		List<MouseInteractionListener> listenersWithoutMouseOver;
		HashSet<MouseInteractionListener> listenersWithLeftMouseDown;
		HashSet<MouseInteractionListener> listenersWithRightMouseDown;
		Transform lastHit;

		static MouseEventSystem instance;

		public static void AddInteractionListener(MouseInteractionListener listener) => GetOrCreateInstance().RegisterListener(listener);
		public static void RemoveInteractionListener(MouseInteractionListener listener) => instance?.DeregisterListener(listener);

		float vid_mouseSmoothT;
		Vector2 vid_mouseSmoothV;
		Vector2 vid_mouseSmoothPos;

		void Awake()
		{
			if (instance == null)
			{
				Init();
			}
			else if (instance != this)
			{
				Debug.Log($"Duplicate Mouse Event System found ({instance.gameObject.name}). Deleting this instance ({gameObject.name}).");
				Destroy(gameObject);
			}
		}

		void Init()
		{
			listenersWithMouseOver = new();
			listenersWithoutMouseOver = new();
			listenersWithLeftMouseDown = new();
			listenersWithRightMouseDown = new();

			lastHit = null;
			instance = this;
			cam ??= Camera.main;//
		}


		void Update()
		{
			if (Application.isEditor)
			{
				vid_mouseSmoothPos = Vector2.SmoothDamp(vid_mouseSmoothPos, Mouse.current.position.ReadValue(), ref vid_mouseSmoothV, vid_mouseSmoothT);
			}
			Transform hitObject = GetObjectUnderMouse();

			if (hitObject != lastHit)
			{
				NotifyMouseExit(lastHit, hitObject);
				NotifyMouseEnter(hitObject);
				lastHit = hitObject;
			}

			HandleMouseButtonEvents();
		}

		void HandleMouseButtonEvents()
		{
			Mouse mouse = Mouse.current;
			// Left mouse down
			if (mouse.leftButton.wasPressedThisFrame)
			{
				LeftMousePressed?.Invoke();
				foreach (var listener in listenersWithMouseOver)
				{
					listener.OnMousePressDown(MouseButton.Left);
					listenersWithLeftMouseDown.Add(listener);
				}
			}
			// Right mouse down
			if (mouse.rightButton.wasPressedThisFrame)
			{
				RightMousePressed?.Invoke();
				foreach (var listener in listenersWithMouseOver)
				{
					listener.OnMousePressDown(MouseButton.Right);
					listenersWithRightMouseDown.Add(listener);
				}
			}
			// Left mouse released
			if (mouse.leftButton.wasReleasedThisFrame)
			{
				LeftMouseReleased?.Invoke();
				foreach (var listener in listenersWithMouseOver)
				{
					listener.OnMouseRelease(MouseButton.Left);
					if (listenersWithLeftMouseDown.Contains(listener))
					{
						listener.OnClickCompleted(MouseButton.Left);
					}
				}
				listenersWithLeftMouseDown.Clear();
			}
			// Right mouse released
			if (mouse.rightButton.wasReleasedThisFrame)
			{
				RightMouseReleased?.Invoke();
				foreach (var listener in listenersWithMouseOver)
				{
					listener.OnMouseRelease(MouseButton.Right);
					if (listenersWithRightMouseDown.Contains(listener))
					{
						listener.OnClickCompleted(MouseButton.Right);
					}
				}
				listenersWithRightMouseDown.Clear();
			}
		}

		void NotifyMouseEnter(Transform enter)
		{
			if (enter is not null)
			{
				// Only consider listeners who aren't already registered as having mouse over them.
				// This is so a listener which is the parent of multiple colliders will only be notified for the first one the mouse enters.
				for (int i = listenersWithoutMouseOver.Count - 1; i >= 0; i--)
				{
					MouseInteractionListener listener = listenersWithoutMouseOver[i];
					if (Belongs(enter, listener))
					{
						listener.OnMouseEnter();
						listenersWithMouseOver.Add(listener);
						listenersWithoutMouseOver.RemoveAt(i);
					}
				}
			}
		}

		void NotifyMouseExit(Transform exit, Transform enter)
		{
			Transform enterTransform = enter?.transform;

			// Only consider listeners who are already registered as having mouse over them (otherwise how could the mouse exit them...)
			for (int i = listenersWithMouseOver.Count - 1; i >= 0; i--)
			{
				MouseInteractionListener listener = listenersWithMouseOver[i];
				// Only consider mouse as exitting the listener if it's not entering another collider that belongs to the listener
				if (enterTransform == null || !Belongs(enterTransform, listener))
				{
					listener.OnMouseExit();
					listenersWithoutMouseOver.Add(listener);
					listenersWithMouseOver.RemoveAt(i);
				}
			}
		}

		bool Belongs(Transform t, MouseInteractionListener listener)
		{
			return t.IsChildOf(listener.transform);
		}

		void RegisterListener(MouseInteractionListener listener)
		{
			if (lastHit != null && Belongs(lastHit.transform, listener))
			{
				listener.OnMouseEnter();
				listenersWithMouseOver.Add(listener);
			}
			else
			{
				listenersWithoutMouseOver.Add(listener);
			}
		}

		void DeregisterListener(MouseInteractionListener listener)
		{
			if (!listenersWithMouseOver.Remove(listener))
			{
				listenersWithoutMouseOver.Remove(listener);
			}

			listenersWithLeftMouseDown.Remove(listener);
			listenersWithRightMouseDown.Remove(listener);
		}

		static MouseEventSystem GetOrCreateInstance()
		{
			if (instance == null)
			{
				instance = FindObjectOfType<MouseEventSystem>();
				if (instance == null)
				{
					GameObject holder = new GameObject("Mouse Event System");
					instance = holder.AddComponent<MouseEventSystem>();
					Debug.Log("No mouse event system was found. Adding to scene.");
				}
				instance.Init();
			}
			return instance;
		}

		Transform GetObjectUnderMouse()
		{
			Mouse mouse = Mouse.current;

			InputSystemUIInputModule uiInputModule = EventSystem.current?.currentInputModule as InputSystemUIInputModule;

			if (uiInputModule is null)
			{
				Vector2 mouseScreenPoint = vid_mouseSmoothPos;
				Vector2 mouseWorldPoint = cam.ScreenToWorldPoint(mouseScreenPoint);
				return Physics2D.OverlapPoint(mouseWorldPoint)?.transform;
			}
			else
			{
				RaycastResult lastRaycastResult = uiInputModule.GetLastRaycastResult(mouse.deviceId);
				return lastRaycastResult.gameObject?.transform;
			}
		}

		public void SetVidMouseSmoothing(float t)
		{
			vid_mouseSmoothT = t;
		}

		public static Vector2 GetMousePos()
		{
			return instance.vid_mouseSmoothPos;
		}

		void OnDestroy()
		{
			if (instance == this)
			{
				instance = null;
			}
		}


	}
}