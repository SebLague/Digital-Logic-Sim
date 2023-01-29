using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipCreation;
using UnityEngine.InputSystem;

namespace DLS.VideoTools
{
	[DefaultExecutionOrder(1000)]
	public class VidHelper : MonoBehaviour
	{
		public bool expandWorkArea;
		public bool showGrid;
		[Header("References")]
		public RectTransform grid;

		ChipEditor editor;
		ProjectManager manager;

		void Start()
		{
			manager = FindObjectOfType<ProjectManager>();
			editor = manager.ActiveEditChipEditor;
			manager.ViewedChipChanged += NewEditor;
			NewEditor(manager.ActiveViewChipEditor);
		}

		void Update()
		{
			grid.gameObject.SetActive(showGrid);
		}

		void NewEditor(ChipEditor viewedEditor)
		{
			if (expandWorkArea)
			{
				manager.ActiveViewChipEditor.WorkArea.VidHelper_ExpandView();
			}
		}
	}
}