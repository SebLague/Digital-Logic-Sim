using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation.UI
{
	public class ContextMenuCreator : MonoBehaviour
	{
		public ProjectManager controller;

		public RectTransform contextMenuHolder;
		public ContextMenu contextMenuPrefab;
		public Palette palette;

		ContextMenu activeContextMenu;
		bool hasOpenContexMenu;
		Vector2 openMenuClickWorldPos;



		void Update()
		{
			ChipEditor activeEditor = controller.ActiveViewChipEditor;
			if (activeEditor is null)
			{
				return;
			}

			if (MouseHelper.RightMousePressedThisFrame() && !activeEditor.AnyControllerBusy())
			{
				if (hasOpenContexMenu)
				{
					activeContextMenu.Close();
				}

				// Context menu for mouse over chip
				if (activeEditor.ChipUnderMouse != null)
				{
					ChipBase chip = activeEditor.ChipUnderMouse;

					activeContextMenu = CreateContextMenu();

					activeContextMenu.SetTitle(chip.Name);
					activeContextMenu.AddButton("View", () => ViewChip(chip));

					if (controller.ActiveViewChipEditor.CanEdit)
					{
						activeContextMenu.AddButton("Delete", () => DeleteChip(chip));
					}
				}
				else if (activeEditor.PinUnderMouse != null && activeEditor.CanEdit)
				{
					Pin pin = activeEditor.PinUnderMouse;
					activeContextMenu = CreateContextMenu();
					string title = "Pin";
					if (!string.IsNullOrWhiteSpace(pin.name) && !string.Equals(pin.PinName, "Pin", System.StringComparison.OrdinalIgnoreCase))
					{
						title += $" ({pin.PinName})";
					}
					activeContextMenu.SetTitle(title);
					foreach (var col in palette.Colours)
					{
						activeContextMenu.AddButton(col.name, () => pin.SetColourTheme(col));
					}
				}
				else if (activeEditor.WireUnderMouse != null && activeEditor.CanEdit)
				{
					Wire wire = activeEditor.WireUnderMouse;
					activeContextMenu = CreateContextMenu();
					activeContextMenu.SetTitle("Wire");
					activeContextMenu.AddButton("Delete", () => wire.DeleteWire());
					activeContextMenu.AddDivider();
					foreach (var col in palette.Colours)
					{
						activeContextMenu.AddButton(col.name, () => wire.SetColourTheme(col));
					}
				}
				else if (activeEditor.WorkArea.WorkAreaMouseInteraction.MouseIsOver)
				{
				}
			}
		}

		ContextMenu CreateContextMenu()
		{
			openMenuClickWorldPos = MouseHelper.GetMouseWorldPosition();
			var contextMenu = Instantiate(contextMenuPrefab, parent: contextMenuHolder);
			contextMenu.MenuClosed += () => hasOpenContexMenu = false;
			contextMenu.SetPosition(MouseHelper.GetMouseScreenPosition());
			hasOpenContexMenu = true;
			return contextMenu;
		}

		void ViewChip(ChipBase chip)
		{
			controller.OpenSubChipViewer(chip);
		}

		void DeleteChip(ChipBase chip)
		{
			chip.Delete();
		}
	}
}