using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Display: BuiltinChip {
	public TextMeshPro txt;
	protected override void Awake()
	{
		base.Awake();
		txt = this.GetComponent<TextMeshPro>();
	}
	void Update() {
		Debug.Log(inputPins[0].State);
		txt.text = System.Convert.ToChar(inputPins[0].State).ToString();
		Debug.Log(inputPins[1].State);
		txt.text += System.Convert.ToChar(inputPins[1].State).ToString();
		Debug.Log(inputPins[2].State);
		txt.text += System.Convert.ToChar(inputPins[2].State).ToString();
		Debug.Log(inputPins[3].State);
		txt.text += System.Convert.ToChar(inputPins[3].State).ToString();
		Debug.Log(inputPins[4].State);
		txt.text += System.Convert.ToChar(inputPins[4].State).ToString();
		Debug.Log(inputPins[5].State);
		txt.text += System.Convert.ToChar(inputPins[5].State).ToString();
		Debug.Log(inputPins[6].State);
		txt.text += System.Convert.ToChar(inputPins[6].State).ToString();
		Debug.Log(inputPins[7].State);
		txt.text += System.Convert.ToChar(inputPins[7].State).ToString();

	}
}