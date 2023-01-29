using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation.UI
{
	public class CursorGuide : MonoBehaviour
	{
		[SerializeField] float thickness = 0.02f;
		[SerializeField] Transform horizontal;
		[SerializeField] Transform vertical;
		[SerializeField] Transform[] extraLines;


		void Start()
		{
			float length = 100;
			horizontal.localScale = new Vector3(length, thickness, 1);
			vertical.localScale = new Vector3(length, thickness, 1);

			foreach (Transform t in extraLines)
			{
				t.localScale = new Vector3(length, thickness, 1);
			}
		}

		public void SetActive(bool isActive)
		{
			gameObject.SetActive(isActive);
			UpdatePosition();
		}

		void LateUpdate()
		{
			UpdatePosition();
		}

		void UpdatePosition()
		{
			transform.position = MouseHelper.GetMouseWorldPosition(transform.position.z);
		}
	}
}