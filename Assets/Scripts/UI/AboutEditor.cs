using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AboutEditor : MonoBehaviour {
	public TMPro.TMP_Text target;

	public CustomCols[] cols;
	public CustomSizes[] sizes;

	TMPro.TMP_Text source;

	void Update () {
		if (!Application.isPlaying) {
			if (source == null) {
				source = GetComponent<TMPro.TMP_Text> ();
			}
			string formattedText = source.text;
			if (cols != null) {
				for (int i = 0; i < cols.Length; i++) {
					string key = $"<color={cols[i].name}>";
					string replace = $"<color=#{ColorUtility.ToHtmlStringRGB (cols[i].colour)}>";
					formattedText = formattedText.Replace (key, replace);
				}
			}

			if (sizes != null) {
				for (int i = 0; i < sizes.Length; i++) {
					string key = $"<size={sizes[i].name}>";
					string replace = $"<size={sizes[i].fontSize}>";
					formattedText = formattedText.Replace (key, replace);
				}
			}

			target.text = formattedText;
		}
	}

	[System.Serializable]
	public struct CustomSizes {
		public string name;
		public int fontSize;
	}

	[System.Serializable]
	public struct CustomCols {
		public string name;
		public Color colour;
	}
}