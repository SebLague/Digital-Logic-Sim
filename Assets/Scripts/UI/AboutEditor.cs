using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AboutEditor : MonoBehaviour {
	public TMPro.TMP_Text target;

	public CustomCols[] cols;

	TMPro.TMP_Text source;

	void Update () {
		if (!Application.isPlaying) {
			if (source == null) {
				source = GetComponent<TMPro.TMP_Text> ();
			}
			string formattedText = source.text;
			for (int i = 0; i < cols.Length; i++) {
				formattedText = formattedText.Replace ("#" + cols[i].name, "#" + ColorUtility.ToHtmlStringRGB (cols[i].colour));
			}

			target.text = formattedText;
		}
	}


	[System.Serializable]
	public struct CustomCols {
		public string name;
		public Color colour;
	}
}