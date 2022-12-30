using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebInput;

public class VidRegButton : MonoBehaviour
{

	public MouseInteraction<GameObject> mouseInteraction;
	public Transform slider;
	public float sliderXLeft;
	public float sliderXRight;
	public bool on;
	// Start is called before the first frame update
	void Awake()
	{
		mouseInteraction = new MouseInteraction<GameObject>(gameObject, gameObject);
		//mouseInteraction.MouseEntered += (e) => GetComponent<MeshRenderer>().sharedMaterial.color = colB;
		//mouseInteraction.MouseExitted += (e) => GetComponent<MeshRenderer>().sharedMaterial.color = colB;
		mouseInteraction.LeftMouseDown += (e) => on = !on;
	}

	// Update is called once per frame
	void Update()
	{
		var pos = slider.localPosition;
		pos.x = Mathf.Lerp(pos.x, on ? sliderXRight : sliderXLeft, Time.deltaTime * 20);
		slider.localPosition = pos;
	}
}
