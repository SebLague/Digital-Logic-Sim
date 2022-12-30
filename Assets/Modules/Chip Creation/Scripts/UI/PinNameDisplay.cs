using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public class PinNameDisplay : MonoBehaviour
	{
		[SerializeField] Transform nameDisplayHolder;
		[SerializeField] TMPro.TMP_Text nameDisplay;
		[SerializeField] Transform nameDisplayBackground;
		[SerializeField] Vector2 backgroundPadding;
		[SerializeField] float spacingAfterPin;

		bool displayToRight;

		public void SetUp(bool displayToRight)
		{
			this.displayToRight = displayToRight;

		}

		public void SetNameVisibility(bool show)
		{
			nameDisplayHolder.gameObject.SetActive(show);
		}

		public bool GetVisibility()
		{
			return nameDisplayHolder.gameObject.activeSelf;
		}

		public void SetText(string text)
		{
			nameDisplay.text = text;

			Vector2 size = nameDisplay.GetPreferredValues();
			nameDisplayBackground.localScale = new Vector3(size.x + backgroundPadding.x, size.y + backgroundPadding.y, 1);
			float posX = (size.x / 2 + spacingAfterPin + backgroundPadding.x / 2) * (displayToRight ? 1 : -1);
			nameDisplayHolder.localPosition = new Vector3(posX, 0, 0);
			nameDisplayHolder.position = new Vector3(nameDisplayHolder.position.x, nameDisplayHolder.position.y, RenderOrder.PinNameDisplay);
		}
	}
}