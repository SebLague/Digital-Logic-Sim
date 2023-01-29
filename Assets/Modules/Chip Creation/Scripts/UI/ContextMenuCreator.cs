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
					CreateChipContextMenu(activeEditor.ChipUnderMouse);

				}
				else if (activeEditor.PinUnderMouse != null && activeEditor.CanEdit)
				{
					CreatePinContextMenu(activeEditor.PinUnderMouse);
				}
				else if (activeEditor.WireUnderMouse != null && activeEditor.CanEdit)
				{
					CreateWireContextMenu(activeEditor.WireUnderMouse);
				}
				else if (activeEditor.WorkArea.WorkAreaMouseInteraction.MouseIsOver)
				{
				}
			}
		}

		void CreateChipContextMenu(ChipBase chip)
		{
			activeContextMenu = CreateContextMenu();

			activeContextMenu.SetTitle(chip.Name);
			activeContextMenu.AddButton("View", () => ViewChip(chip));

			if (controller.ActiveViewChipEditor.CanEdit)
			{
				activeContextMenu.AddButton("Delete", () => DeleteChip(chip));
			}
		}

		void CreatePinContextMenu(Pin pin)
		{
			if (!pin.IsBusPin)
			{
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
		}

		void CreateWireContextMenu(Wire wire)
		{
			activeContextMenu = CreateContextMenu();
			string title = wire.IsBusWire ? "Bus Line" : "Wire";
			if (!wire.IsBusWire && wire.SourcePin.IsBusPin)
			{
				title = "Wire (from bus)";
			}
			activeContextMenu.SetTitle(title);
			activeContextMenu.AddButton("Delete", () => wire.DeleteWire());
			// Bus inherits colour from inputs, so don't show colour menu
			bool hasColourOptions = !wire.IsBusWire && !wire.SourcePin.IsBusPin;
			// Create wire colour options
			if (hasColourOptions)
			{
				activeContextMenu.AddDivider();
				foreach (var col in palette.Colours)
				{
					activeContextMenu.AddButton(col.name, () => wire.SetColourTheme(col));
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