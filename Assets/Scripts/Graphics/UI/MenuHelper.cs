using System;
using DLS.Game;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class MenuHelper
	{
		public enum CancelConfirmResult
		{
			None,
			Cancel,
			Confirm
		}

		static readonly string[] CancelConfirmButtonNames = { "CANCEL", "CONFIRM" };
		static readonly bool[] CancelConfirmInteractableState = { true, true };

		static readonly string[] ButtonGroupNames = { "A", "B", "C" };
		static readonly bool[] ButtonGroupInteractableStates = { true, true, true };

		public static UIThemeDLS Theme => ActiveUITheme;

		public static Vector2 DrawLabelSectionOfLabelInputPair(Vector2 topLeft, Vector2 size, string label, Color labelCol, bool drawBackground)
		{
			const float pad = 1;
			if (drawBackground) UI.DrawPanel(topLeft, size, Color.red * 0.1f, Anchor.TopLeft);
			Vector2 centreLeft = topLeft + Vector2.down * size.y / 2;
			UI.DrawText(label, Theme.FontRegular, Theme.FontSizeRegular, centreLeft + Vector2.right * pad, Anchor.TextCentreLeft, labelCol);
			Vector2 centreRight = centreLeft + Vector2.right * size.x;
			return centreRight;
		}

		public static int LabeledOptionsWheel(string label, Color labelCol, Vector2 topLeft, Vector2 size, UIHandle id, string[] wheelOptions, float wheelWidth, bool drawBackground = false)
		{
			Vector2 centreRight = DrawLabelSectionOfLabelInputPair(topLeft, size, label, labelCol, drawBackground);
			int wheelIndex = UI.WheelSelector(id, wheelOptions, centreRight, new Vector2(wheelWidth, size.y), Theme.OptionsWheel, Anchor.CentreRight);
			UI.OverridePreviousBounds(Bounds2D.CreateFromTopLeftAndSize(topLeft, size));
			return wheelIndex;
		}

		public static InputFieldState LabeledInputField(string label, Color labelCol, Vector2 topLeft, Vector2 size, UIHandle id, Func<string, bool> validation, float inputFieldWidth, bool drawBackground = false)
		{
			InputFieldTheme inputFieldTheme = Theme.ChipNameInputField;
			inputFieldTheme.fontSize = Theme.FontSizeRegular;

			Vector2 centreRight = DrawLabelSectionOfLabelInputPair(topLeft, size, label, labelCol, drawBackground);
			InputFieldState state = UI.InputField(id, inputFieldTheme, centreRight, new Vector2(inputFieldWidth, size.y), string.Empty, Anchor.CentreRight, 1, validation);
			UI.OverridePreviousBounds(Bounds2D.CreateFromTopLeftAndSize(topLeft, size));
			return state;
		}

		public static void DrawText(string text, Vector2 pos, Anchor anchor, Color col, bool bold = false)
		{
			FontType font = bold ? Theme.FontBold : Theme.FontRegular;
			UI.DrawText(text, font, Theme.FontSizeRegular, pos, anchor, col);
		}

		public static void DrawCentredTextWithBackground(string text, Vector2 pos, Vector2 size, Anchor anchor, Color col, Color bgCol, bool bold = false)
		{
			UI.DrawPanel(pos, size, bgCol, anchor);
			Bounds2D bgBounds = UI.PrevBounds;
			DrawText(text, bgBounds.Centre, Anchor.TextFirstLineCentre, col, bold);
			UI.OverridePreviousBounds(bgBounds);
		}

		public static void DrawLeftAlignTextWithBackground(string text, Vector2 pos, Vector2 size, Anchor anchor, Color col, Color bgCol, bool bold = false, float textPadX = 1)
		{
			UI.DrawPanel(pos, size, bgCol, anchor);
			Bounds2D bgBounds = UI.PrevBounds;
			DrawText(text, bgBounds.CentreLeft + Vector2.right * textPadX, Anchor.TextCentreLeft, col, bold);
			UI.OverridePreviousBounds(bgBounds);
		}

		public static void DrawBackgroundOverlay()
		{
			UI.DrawFullscreenPanel(Theme.MenuBackgroundOverlayCol);
		}

		public static void DrawReservedMenuPanel(Draw.ID panelID, Bounds2D contentBounds, bool pad = true)
		{
			if (pad) contentBounds = Bounds2D.Grow(contentBounds, PanelUIPadding);
			UI.ModifyPanel(panelID, contentBounds, Theme.MenuPanelCol);


			Color outlineCol = ColHelper.MakeCol(0.26f);
			float outlineWidth = 0.05f;

			UI.DrawLine(contentBounds.BottomLeft, contentBounds.TopLeft, outlineWidth, outlineCol);
			UI.DrawLine(contentBounds.TopLeft, contentBounds.TopRight, outlineWidth, outlineCol);
			UI.DrawLine(contentBounds.BottomRight, contentBounds.TopRight, outlineWidth, outlineCol);
			UI.DrawLine(contentBounds.BottomRight, contentBounds.BottomLeft, outlineWidth, outlineCol);
		}

		public static int DrawButtonPair(string nameA, string nameB, Vector2 topLeft, float width, bool addVerticalPadding, bool interactableA = true, bool interactableB = true, bool ignoreInputs = false)
		{
			if (addVerticalPadding) topLeft += Vector2.down * (DefaultButtonSpacing * 3);

			ButtonGroupNames[0] = nameA;
			ButtonGroupNames[1] = nameB;
			ButtonGroupInteractableStates[0] = interactableA;
			ButtonGroupInteractableStates[1] = interactableB;

			int buttonIndex = UI.HorizontalButtonGroup(ButtonGroupNames.AsSpan(0, 2), ButtonGroupInteractableStates.AsSpan(0, 2), Theme.ButtonTheme, topLeft, new Vector2(width, ButtonHeight), DefaultButtonSpacing, 0, Anchor.TopLeft, ignoreInputs: ignoreInputs);
			return buttonIndex;
		}

		public static int DrawButtonTriplet(string nameA, string nameB, string nameC, Vector2 topLeft, float width, bool addVerticalPadding, bool interactableA = true, bool interactableB = true, bool interactableC = true, bool ignoreInputs = false)
		{
			if (addVerticalPadding) topLeft += Vector2.down * (DefaultButtonSpacing * 3);

			ButtonGroupNames[0] = nameA;
			ButtonGroupNames[1] = nameB;
			ButtonGroupNames[2] = nameC;
			ButtonGroupInteractableStates[0] = interactableA;
			ButtonGroupInteractableStates[1] = interactableB;
			ButtonGroupInteractableStates[2] = interactableC;

			int buttonIndex = UI.HorizontalButtonGroup(ButtonGroupNames.AsSpan(0, 3), ButtonGroupInteractableStates.AsSpan(0, 3), Theme.ButtonTheme, topLeft, new Vector2(width, ButtonHeight), DefaultButtonSpacing, 0, Anchor.TopLeft, ignoreInputs: ignoreInputs);
			return buttonIndex;
		}

		public static CancelConfirmResult DrawCancelConfirmButtons(Vector2 topLeft, float width, bool addVerticalPadding, bool useKeyboardShortcuts = true, bool canCancel = true, bool canConfirm = true)
		{
			if (addVerticalPadding) topLeft += Vector2.down * (DefaultButtonSpacing * 3);
			const int CancelIndex = 0;
			const int ConfirmIndex = 1;

			CancelConfirmInteractableState[CancelIndex] = canCancel;
			CancelConfirmInteractableState[ConfirmIndex] = canConfirm;
			int buttonIndex = UI.HorizontalButtonGroup(CancelConfirmButtonNames, CancelConfirmInteractableState, Theme.ButtonTheme, topLeft, width, DefaultButtonSpacing, 0, Anchor.TopLeft);

			if (useKeyboardShortcuts)
			{
				if (canCancel && KeyboardShortcuts.CancelShortcutTriggered) buttonIndex = CancelIndex;
				if (canConfirm && KeyboardShortcuts.ConfirmShortcutTriggered) buttonIndex = ConfirmIndex;
			}

			return buttonIndex switch
			{
				CancelIndex => CancelConfirmResult.Cancel,
				ConfirmIndex => CancelConfirmResult.Confirm,
				_ => CancelConfirmResult.None
			};
		}
	}
}