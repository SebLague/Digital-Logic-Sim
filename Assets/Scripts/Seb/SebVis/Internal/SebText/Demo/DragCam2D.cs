using UnityEngine;

namespace Seb.Vis.Text.Demo
{
	public class DragCam2D : MonoBehaviour
	{
		[SerializeField] float zoomSpeed = 0.2f;
		[SerializeField] Vector2 zoomRange = new(0.0001f, 1000);
		[SerializeField] bool zoomToMouse = true;

		Camera cam;
		Vector2 mouseDragScreenPosOld;

		void Start()
		{
			cam = GetComponent<Camera>();
			Debug.Log("Middle-mouse drag to move camera. Q/E or middle-mouse wheel to zoom");
		}

		void LateUpdate()
		{
			Vector2 mouseScreenPos = Input.mousePosition;
			Vector2 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);

			// Pan
			if (Input.GetMouseButtonDown(2))
			{
				mouseDragScreenPosOld = mouseScreenPos;
			}

			if (Input.GetMouseButton(2))
			{
				Vector2 mouseWorldPosOld = cam.ScreenToWorldPoint(mouseDragScreenPosOld);
				Vector3 delta = mouseWorldPosOld - mouseWorldPos;
				bool lockHorizontal = Input.GetKey(KeyCode.LeftShift);
				if (lockHorizontal) delta.x = 0;
				transform.position += delta;
				mouseDragScreenPosOld = mouseScreenPos;
			}

			Vector2 mouseWorldPosAfterPanning = cam.ScreenToWorldPoint(mouseScreenPos);


			// Zoom
			float deltaZoom = -GetScrollInput() * cam.orthographicSize * zoomSpeed * 0.5f;
			if (Input.GetKey(KeyCode.Q))
			{
				deltaZoom = -1 * cam.orthographicSize * zoomSpeed * 10f * Time.deltaTime;
			}

			if (Input.GetKey(KeyCode.E))
			{
				deltaZoom = 1 * cam.orthographicSize * zoomSpeed * 10f * Time.deltaTime;
			}

			cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + deltaZoom, zoomRange.x, zoomRange.y);
			// Adjust cam pos to centre zoom on mouse
			if (zoomToMouse)
			{
				Vector2 mouseWorldPosAfterZoom = cam.ScreenToWorldPoint(mouseScreenPos);
				transform.position += (Vector3)(mouseWorldPosAfterPanning - mouseWorldPosAfterZoom);
			}
		}

		float GetScrollInput() => Input.mouseScrollDelta.y;
	}
}