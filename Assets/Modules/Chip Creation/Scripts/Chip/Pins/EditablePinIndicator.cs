using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebInput;

public class EditablePinIndicator : MonoBehaviour
{
	public MouseInteraction<EditablePinIndicator> MouseInteraction { get; private set; }

	[SerializeField] MeshRenderer display;

	void Awake()
	{
		display.material = Material.Instantiate(display.sharedMaterial);
		MouseInteraction = new MouseInteraction<EditablePinIndicator>(gameObject, this);
	}

	public void SetColour(Color col)
	{
		display.sharedMaterial.color = col;
	}
}
