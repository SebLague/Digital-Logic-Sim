using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.MainMenu
{
	public class TitleColCycle : MonoBehaviour
	{
		public Color[] cols;
		public float cycleDuration;
		public float cycleDelay;
		public TMPro.TMP_Text text;

		string textString;
		Color[] charCols;
		int colIndex;
		float time;

		void Start()
		{
			textString = text.text;
			charCols = new Color[textString.Length];
			for (int i = 0; i < charCols.Length; i++)
			{
				charCols[i] = cols[0];
			}
			colIndex = 1;
			time = -cycleDelay / cycleDuration / 2;
		}

		// Update is called once per frame
		void Update()
		{
			time += Time.deltaTime / cycleDuration;
			//int i = (int)((Time.time / t) * cols.Length) % cols.Length;
			//text.color = cols[i];
			if (time >= 0)
			{
				int charIndex = (int)(time * charCols.Length) % charCols.Length;
				charCols[charIndex] = cols[colIndex];
			}
			if (time >= 1)
			{
				time = -cycleDelay / cycleDuration;
				colIndex++;
				colIndex %= cols.Length;
			}


			string formatted = "";
			for (int i = 0; i < textString.Length; i++)
			{
				formatted += $"<color=#{ColorUtility.ToHtmlStringRGB(charCols[i])}>{textString[i]}</color>";
			}
			text.text = formatted;

		}
	}
}