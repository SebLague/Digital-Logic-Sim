using TMPro;
using UnityEngine;

public class PinNameDisplay : MonoBehaviour {

	public TMP_Text nameUI;
	public Transform background;
	public Vector2 backgroundPadding;

	public void Set (Pin pin) {

		if (string.IsNullOrEmpty (pin.pinName)) {
			nameUI.text = "UNNAMED PIN";
		} else {
			nameUI.text = pin.pinName;
		}

		float backgroundSizeX = nameUI.preferredWidth + backgroundPadding.x;
		float backgroundSizeY = nameUI.preferredHeight + backgroundPadding.y;
		background.localScale = new Vector3 (backgroundSizeX, backgroundSizeY, 1);

		float spacingFromPin = (backgroundSizeX / 2 + Pin.interactionRadius * 1.5f);
		spacingFromPin *= (pin.pinType == Pin.PinType.ChipInput) ? -1 : 1;
		transform.position = pin.transform.position + Vector3.right * spacingFromPin + Vector3.forward * -1;
	}
}