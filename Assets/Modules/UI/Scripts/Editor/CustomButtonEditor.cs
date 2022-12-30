using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DLS.ChipCreation.UI.EditorScripts
{
	[CustomEditor(typeof(CustomButton), true)]
	public class CustomButtonEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Set GameObject Name"))
			{
				CustomButton button = target as CustomButton;
				button.gameObject.name = $"{button.GetButtonText()} (Button)";
			}
		}
	}
}