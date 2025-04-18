using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.Game;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class ChipCustomizationMenu
	{
		static readonly string[] nameDisplayOptions =
		{
			"Name: Middle",
			"Name: Top",
			"Name: Hidden"
		};


		// ---- State ----
		static SubChipInstance[] subChipsWithDisplays;
		static string displayLabelString;
		static string colHexCodeString;

		static readonly UIHandle ID_DisplaysScrollView = new("CustomizeMenu_DisplaysScroll");
		static readonly UIHandle ID_ColourPicker = new("CustomizeMenu_ChipCol");
		static readonly UIHandle ID_ColourHexInput = new("CustomizeMenu_ChipColHexInput");
		static readonly UIHandle ID_NameDisplayOptions = new("CustomizeMenu_NameDisplayOptions");
		static readonly UI.ScrollViewDrawElementFunc drawDisplayScrollEntry = DrawDisplayScroll;
		static readonly Func<string, bool> hexStringInputValidator = ValidateHexStringInput;

		public static void OnMenuOpened()
		{
			DevChipInstance chip = Project.ActiveProject.ViewedChip;
			subChipsWithDisplays = chip.GetSubchips().Where(c => c.Description.HasDisplay()).OrderBy(c => c.Position.x).ThenBy(c => c.Position.y).ToArray();
			CustomizationSceneDrawer.OnCustomizationMenuOpened();
			displayLabelString = $"DISPLAYS ({subChipsWithDisplays.Length}):";

			InitUIFromChipDescription();
		}

		public static void DrawMenu()
		{
			// Don't draw menu when placing display
			if (CustomizationSceneDrawer.IsPlacingDisplay) return;

			const float width = 20;
			const float pad = UILayoutHelper.DefaultSpacing;
			const float pw = width - pad * 2;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			UI.DrawPanel(UI.TopLeft, new Vector2(width, UI.Height), theme.MenuPanelCol, Anchor.TopLeft);

			// ---- Cancel/confirm buttons ----
			int cancelConfirmButtonIndex = MenuHelper.DrawButtonPair("CANCEL", "CONFIRM", UI.TopLeft + Vector2.down * pad, pw, false);

			// ---- Chip name UI ----
			int nameDisplayMode = UI.WheelSelector(ID_NameDisplayOptions, nameDisplayOptions, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight), theme.OptionsWheel, Anchor.TopLeft);
			ChipSaveMenu.ActiveCustomizeDescription.NameLocation = (NameDisplayLocation)nameDisplayMode;

			// ---- Chip colour UI ----
			Color newCol = UI.DrawColourPicker(ID_ColourPicker, NextPos(), pw, Anchor.TopLeft);
			InputFieldTheme inputTheme = MenuHelper.Theme.ChipNameInputField;
			inputTheme.fontSize = MenuHelper.Theme.FontSizeRegular;

			InputFieldState hexColInput = UI.InputField(ID_ColourHexInput, inputTheme, NextPos(), new Vector2(pw, DrawSettings.ButtonHeight), "#", Anchor.TopLeft, 1, hexStringInputValidator);

			if (newCol != ChipSaveMenu.ActiveCustomizeDescription.Colour)
			{
				ChipSaveMenu.ActiveCustomizeDescription.Colour = newCol;
				UpdateChipColHexStringFromColour(newCol);
			}
			else if (colHexCodeString != hexColInput.text)
			{
				UpdateChipColFromHexString(hexColInput.text);
			}

			// ---- Displays UI ----
			Color labelCol = ColHelper.Darken(theme.MenuPanelCol, 0.01f);
			Vector2 labelPos = NextPos(1);
			UI.TextWithBackground(labelPos, new Vector2(pw, DrawSettings.ButtonHeight), Anchor.TopLeft, displayLabelString, theme.FontBold, theme.FontSizeRegular, Color.white, labelCol);

			float scrollViewHeight = 20;
			float scrollViewSpacing = UILayoutHelper.DefaultSpacing;
			UI.DrawScrollView(ID_DisplaysScrollView, NextPos(), new Vector2(pw, scrollViewHeight), scrollViewSpacing, Anchor.TopLeft, theme.ScrollTheme, drawDisplayScrollEntry, subChipsWithDisplays.Length);

			Vector2 NextPos(float extraPadding = 0)
			{
				return UI.PrevBounds.BottomLeft + Vector2.down * (pad + extraPadding);
			}

			// Cancel
			if (cancelConfirmButtonIndex == 0)
			{
				RevertChanges();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
			// Confirm
			else if (cancelConfirmButtonIndex == 1)
			{
				UpdateCustomizeDescription();
				UIDrawer.SetActiveMenu(UIDrawer.MenuType.ChipSave);
			}
		}

		static void DrawDisplayScroll(Vector2 pos, float width, int i, bool isLayoutPass)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			SubChipInstance subChip = subChipsWithDisplays[i];
			ChipDescription chipDesc = subChip.Description;
			string label = subChip.Label;
			string displayName = string.IsNullOrWhiteSpace(label) ? chipDesc.Name : label;

			// Don't allow adding same display multiple times
			bool enabled = CustomizationSceneDrawer.SelectedDisplay == null || subChip.ID != CustomizationSceneDrawer.SelectedDisplay.Desc.SubChipID; // display is removed from list when selected, so check manually here
			foreach (DisplayInstance d in ChipSaveMenu.ActiveCustomizeChip.Displays)
			{
				if (d.Desc.SubChipID == subChip.ID)
				{
					enabled = false;
					break;
				}
			}

			// Display selected, start placement
			if (UI.Button(displayName, theme.ButtonTheme, pos, new Vector2(width, 0), enabled, false, true, Anchor.TopLeft))
			{
				SubChipDescription subChipDesc = new(chipDesc.Name, subChipsWithDisplays[i].ID, string.Empty, Vector2.zero, null);
				SubChipInstance instance = new(chipDesc, subChipDesc);
				CustomizationSceneDrawer.StartPlacingDisplay(instance);
			}
		}

		static void RevertChanges()
		{
			ChipSaveMenu.RevertCustomizationStateToBeforeEnteringCustomizeMenu();
			InitUIFromChipDescription();
		}

		static void InitUIFromChipDescription()
		{
			// Init col picker to chip colour
			ColourPickerState chipColourPickerState = UI.GetColourPickerState(ID_ColourPicker);
			Color.RGBToHSV(ChipSaveMenu.ActiveCustomizeDescription.Colour, out chipColourPickerState.hue, out chipColourPickerState.sat, out chipColourPickerState.val);
			UpdateChipColHexStringFromColour(chipColourPickerState.GetRGB());

			// Init name display mode
			WheelSelectorState nameDisplayWheelState = UI.GetWheelSelectorState(ID_NameDisplayOptions);
			nameDisplayWheelState.index = (int)ChipSaveMenu.ActiveCustomizeDescription.NameLocation;
		}

		static void UpdateCustomizeDescription()
		{
			List<DisplayInstance> displays = ChipSaveMenu.ActiveCustomizeChip.Displays;
			ChipSaveMenu.ActiveCustomizeDescription.Displays = displays.Select(s => s.Desc).ToArray();
		}

		static void UpdateChipColHexStringFromColour(Color col)
		{
			int colInt = (byte)(col.r * 255) << 16 | (byte)(col.g * 255) << 8 | (byte)(col.b * 255);
			colHexCodeString = "#" + $"{colInt:X6}";
			UI.GetInputFieldState(ID_ColourHexInput).SetText(colHexCodeString, false);
		}

		static void UpdateChipColFromHexString(string hexString)
		{
			colHexCodeString = hexString;
			hexString = hexString.Replace("#", "");
			hexString = hexString.PadRight(6, '0');

			if (ColHelper.TryParseHexCode(hexString, out Color col))
			{
				UI.GetColourPickerState(ID_ColourPicker).SetRGB(col);
				ChipSaveMenu.ActiveCustomizeDescription.Colour = col;
			}
		}

		static bool ValidateHexStringInput(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return true;

			int numHexDigits = 0;

			for (int i = 0; i < text.Length; i++)
			{
				if (i == 0 && text[i] == '#') continue;

				if (Uri.IsHexDigit(text[i]))
				{
					numHexDigits++;
				}
				else return false;
			}

			return numHexDigits <= 6;
		}
	}
}