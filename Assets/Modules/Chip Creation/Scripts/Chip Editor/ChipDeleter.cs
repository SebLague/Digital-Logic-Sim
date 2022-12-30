using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	public class ChipDeleter : ControllerBase
	{

		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);
		}

		void Update()
		{
			if (chipEditor.CanEdit)
			{
				HandleInput();
			}
		}

		void HandleInput()
		{
			if (Keyboard.current.backspaceKey.wasPressedThisFrame)
			{
				DeleteSelectedChips();
			}
		}

		void DeleteSelectedChips()
		{
			ChipBase[] chipsToDelete = new List<ChipBase>(chipEditor.ChipSelector.SelectedChips).ToArray();
			foreach (var chip in chipsToDelete)
			{
				chip.Delete();
			}
		}
	}
}