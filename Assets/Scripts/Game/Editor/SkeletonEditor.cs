using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (ChipSkeleton))]
public class SkeletonEditor : Editor {

	ChipSkeleton skeleton;
	Editor chipSpecEditor;
	bool foldout;

	public override void OnInspectorGUI () {

		DrawDefaultInspector ();

		if (skeleton.chipSpec) {
			using (var check = new EditorGUI.ChangeCheckScope ()) {
				DrawSettingsEditor (skeleton.chipSpec, ref foldout, ref chipSpecEditor);
				if (check.changed) {
					skeleton.SpecUpdated ();
				}
			}

			SaveState ();
		}
	}

	void DrawSettingsEditor (Object settings, ref bool foldout, ref Editor editor) {
		if (settings != null) {
			foldout = EditorGUILayout.InspectorTitlebar (foldout, settings);
			if (foldout) {
				CreateCachedEditor (settings, null, ref editor);
				editor.OnInspectorGUI ();
			}
		}
	}

	private void OnEnable () {
		skeleton = (ChipSkeleton) target;
		foldout = EditorPrefs.GetBool (PrefsSaveName (foldout), true);
	}

	void SaveState () {
		EditorPrefs.SetBool (PrefsSaveName (foldout), foldout);
	}

	string PrefsSaveName (object a) {
		return nameof (a) + "_" + skeleton.GetInstanceID ();
	}
}