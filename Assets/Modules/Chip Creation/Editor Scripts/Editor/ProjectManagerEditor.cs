using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DLS.ChipCreation.UnityEditorScripts
{
	[CustomEditor(typeof(ProjectManager))]
	public class ProjectManagerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Open Save Folder"))
			{
				EditorUtility.RevealInFinder(SavePaths.AllData);
			}


			using (EditorGUI.DisabledGroupScope scope = new(!Application.isPlaying))
			{
				if (GUILayout.Button("Resave All"))
				{
					if (Application.isPlaying)
					{
						(target as ProjectManager).ResaveAll();
					}
				}
			}

		}
	}
}