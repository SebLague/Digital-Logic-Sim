using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DLS.MainMenu.CustomEditors
{
	[CustomEditor(typeof(MainMenuController))]
	public class MainMenuEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			MainMenuController controller = target as MainMenuController;

			GUILayout.Space(15);
			
			ScenePicker(controller);
			ActiveMenuPicker(controller);
		}

		void ActiveMenuPicker(MainMenuController controller)
		{
			Undo.RecordObject(controller, "Set Menu Index");
			controller.StartUpMenuIndex = EditorGUILayout.Popup("Startup Menu", controller.StartUpMenuIndex, controller.GetAllMenuNames());
			int activeMenuIndex = controller.ActiveMenuIndex;
			int newActiveMenuIndex = EditorGUILayout.Popup("Active Menu", activeMenuIndex, controller.GetAllMenuNames());
			if (activeMenuIndex != newActiveMenuIndex)
			{
				controller.ActiveMenuIndex = newActiveMenuIndex;
			}
		}

		void ScenePicker(MainMenuController controller)
		{
			SceneAsset currentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(controller.SceneToLoadOnPlay);
			SceneAsset newScene = EditorGUILayout.ObjectField("Scene To Play", currentScene, typeof(SceneAsset), false) as SceneAsset;

			if (newScene != currentScene)
			{
				Undo.RecordObject(controller, "Set Scene To Play");
				controller.SceneToLoadOnPlay = AssetDatabase.GetAssetPath(newScene);
			}
		}
	}
}