using System;
using System.Collections.Generic;
using System.Text;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis.Text.Rendering;
using UnityEngine;
using static Seb.Vis.UI.UILayoutHelper;

namespace Seb.Vis.UI
{
	public static class UI
	{
		public delegate void ScrollViewDrawContentFunc(Vector2 topLeft, float width, bool isLayoutOnly);

		public delegate void ScrollViewDrawElementFunc(Vector2 topLeft, float width, int index, bool isLayoutOnly);

		const string MString = "M";

		public const float Width = 100;
		public const float HalfWidth = Width / 2;

		static readonly Scope<BoundsScope> boundsScopes = new();
		static readonly Scope<UIScope> uiScopes = new();
		static readonly Scope<DisabledScope> disabledScopes = new();

		// -- State lookups --
		static readonly Dictionary<UIHandle, InputFieldState> inputFieldStates = new();
		static readonly Dictionary<UIHandle, ButtonState> buttonStates = new();
		static readonly Dictionary<UIHandle, ColourPickerState> colPickerStates = new();
		static readonly Dictionary<UIHandle, ScrollBarState> scrollbarStates = new();
		static readonly Dictionary<UIHandle, WheelSelectorState> wheelSelectorStates = new();
		static readonly Dictionary<UIHandle, bool> checkBoxStates = new();


		// Scroll draw state
		static readonly ScrollViewDrawContentFunc drawScrollContent = DrawScrollContent;
		static ScrollViewDrawElementFunc scollContext_activeScrollDrawElementFunc;
		static int scollContext_activeScrollDrawCount;
		static float scollContext_activeScrollDrawSpacing;

		//
		static int mouseOverUIFrameIndex;

		static UIScope currUIScope => uiScopes.CurrentScope;
		static Vector2 canvasBottomLeft => currUIScope.canvasBottomLeft;
		public static float Height => Width * currUIScope.aspect;
		public static float HalfHeight => Height * 0.5f;
		public static Vector2 Centre => new Vector2(Width, Height) * 0.5f;
		public static Vector2 CentreTop => new(Width * 0.5f, Height);
		public static Vector2 CentreBottom => new(Width * 0.5f, 0);
		public static Vector2 TopLeft => new(0, Height);
		public static Vector2 TopRight => new(Width, Height);
		public static Vector2 BottomLeft => new(0, 0);
		public static Vector2 BottomRight => new(Width, 0);

		// Canvas size in screenspace
		static Vector2 canvasSize => currUIScope.canvasSize;
		static Vector2 screenSize => currUIScope.screenSize;
		static float scale => currUIScope.scale; // Scale to go from UISpace to ScreenSpace
		static float invScale => currUIScope.invScale;

		public static Bounds2D PrevBounds { get; private set; }
		static bool forceInteractionDisabled => disabledScopes.IsInsideScope && disabledScopes.CurrentScope.IsDisabled;

		static bool IsRendering =>
			!boundsScopes.IsInsideScope || boundsScopes.CurrentScope.DrawUI;

		public static bool IsMouseOverUIThisFrame => mouseOverUIFrameIndex == Time.frameCount;

		//  --------------------------- UI Scope functions ---------------------------

		// Begin drawing full-screen UI
		public static UIScope CreateUIScope() => CreateUIScope(Vector2.zero, new Vector2(Screen.width, Screen.height), false);

		// Begin drawing UI with fixed aspect ratio. If aspect doesn't match screen aspect, letterboxes can optionally be displayed.
		public static UIScope CreateFixedAspectUIScope(int aspectX = 16, int aspectY = 9, bool drawLetterbox = true)
		{
			Vector2 screenSize = new(Screen.width, Screen.height);
			// Display size is narrower than desired aspect, must add top/bottom bars
			if (Screen.width * aspectY < Screen.height * aspectX)
			{
				float canvasWidth = Screen.width;
				float canvasHeight = canvasWidth * aspectY / aspectX;
				float bottomY = (Screen.height - canvasHeight) / 2f;
				return CreateUIScope(new Vector2(0, bottomY), new Vector2(canvasWidth, canvasHeight), drawLetterbox);
			}
			// Display size is wider than desired aspect, must add left/right bars

			if (Screen.width * aspectY > Screen.height * aspectX)
			{
				float canvasHeight = Screen.height;
				float canvasWidth = canvasHeight * aspectX / aspectY;
				float leftX = (Screen.width - canvasWidth) / 2f;
				return CreateUIScope(new Vector2(leftX, 0), new Vector2(canvasWidth, canvasHeight), drawLetterbox);
			}
			// Display size has desired aspect ratio, no bars required

			return CreateUIScope(Vector2.zero, screenSize, false);
		}

		// Begin drawing UI with custom size and position on the screen
		public static UIScope CreateUIScope(Vector2 bottomLeft, Vector2 size, bool drawLetterboxes)
		{
			UIScope scope = uiScopes.CreateScope();
			scope.canvasBottomLeft = bottomLeft;
			scope.canvasSize = size;
			scope.screenSize = new Vector2(Screen.width, Screen.height);
			scope.scale = size.x / 100f;
			scope.invScale = 1f / scope.scale;
			scope.drawLetterboxes = drawLetterboxes;
			scope.aspect = size.y / size.x;
			Draw.StartLayer(Vector2.zero, 1, true);

			return scope;
		}

		public static DisabledScope BeginDisabledScope(bool disabled)
		{
			DisabledScope scope = disabledScopes.CreateScope();
			scope.Init(disabled, disabledScopes);
			return scope;
		}

		public static void StartNewLayer()
		{
			Draw.StartLayer(Vector2.zero, 1, true);
		}

		// Creates a scope in which the bounding box of all UI elements is tracked.
		// If draw is set to false, elements will not be rendered; only the bounds will be calculated.
		// Usage: using (UI.CreateBoundsScope(draw = true)) { var bounds = UI.GetCurrentBoundsScope(); }
		public static BoundsScope BeginBoundsScope(bool draw)
		{
			BoundsScope scope = boundsScopes.CreateScope();
			scope.Init(draw);
			return scope;
		}

		public static Draw.MaskScope CreateMaskScopeMinMax(Vector2 maskMin, Vector2 maskMax)
		{
			Vector2 maskMin_ss = UIToScreenSpace(maskMin);
			Vector2 maskMax_ss = UIToScreenSpace(maskMax);

			// Clip new mask to bounds of parent mask
			(Vector2 parentMin_ss, Vector2 parentMax_ss) = Draw.GetActiveMaskMinMax();
			maskMin_ss = Vector2.Max(maskMin_ss, parentMin_ss);
			maskMax_ss = Vector2.Min(maskMax_ss, parentMax_ss);

			return Draw.BeginMaskScope(maskMin_ss, maskMax_ss);
		}

		public static Draw.MaskScope CreateMaskScope(Vector2 centre, Vector2 size) => CreateMaskScopeMinMax(centre - size / 2, centre + size / 2);

		public static Draw.MaskScope CreateMaskScope(Bounds2D bounds) => CreateMaskScopeMinMax(bounds.Min, bounds.Max);

		// Get the bounding box of the current bounds scope.
		// Note: a scope must have been created with CreateBoundsScope()
		public static Bounds2D GetCurrentBoundsScope() => boundsScopes.CurrentScope.GetBounds();

		//  --------------------------- Draw functions ---------------------------

		public static void DrawSlider(Vector2 pos, Vector2 size, Anchor anchor, ref SliderState state)
		{
			Vector2 centre = CalculateCentre(pos, size, anchor);
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(pos, size);

			Draw.Quad(ss.centre, ss.size, Color.white);


			Vector2 handlePos_ss = Vector2.Lerp(ss.centre + Vector2.left * ss.size.x / 2, ss.centre + Vector2.right * ss.size.x / 2, state.progressT);
			float handleSize_ss = ss.size.y * 1.5f;

			bool mouseOverHandle = InputHelper.MouseInPoint_ScreenSpace(handlePos_ss, handleSize_ss);
			if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && mouseOverHandle)
			{
				state.handleSelected = true;
			}
			else if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
			{
				state.handleSelected = false;
			}

			if (state.handleSelected)
			{
				float minX = ss.centre.x - ss.size.x / 2;
				float maxX = ss.centre.x + ss.size.x / 2;
				state.progressT = (InputHelper.MousePos.x - minX) / (maxX - minX);
			}

			Draw.Point(handlePos_ss, handleSize_ss, mouseOverHandle || state.handleSelected ? Color.red : Color.yellow);
			OnFinishedDrawingUIElement(centre, size);
		}


		public static ScrollBarState DrawScrollView(UIHandle scrollID, Vector2 pos, Vector2 size, float spacing, Anchor anchor, ScrollViewTheme theme, ScrollViewDrawElementFunc drawElementFunc, int count)
		{
			scollContext_activeScrollDrawElementFunc = drawElementFunc;
			scollContext_activeScrollDrawCount = count;
			scollContext_activeScrollDrawSpacing = spacing;
			return DrawScrollView(scrollID, pos, size, anchor, theme, drawScrollContent);
		}

		static void DrawScrollContent(Vector2 topLeft, float width, bool isLayoutOnly)
		{
			for (int i = 0; i < scollContext_activeScrollDrawCount; i++)
			{
				scollContext_activeScrollDrawElementFunc.Invoke(topLeft, width, i, isLayoutOnly);
				topLeft.y = PrevBounds.Bottom - scollContext_activeScrollDrawSpacing;
			}
		}

		public static ScrollBarState DrawScrollView(UIHandle scrollID, Vector2 pos, Vector2 size, Anchor anchor, ScrollViewTheme theme, ScrollViewDrawContentFunc drawContentFunc)
		{
			ScrollBarState scrollState = GetScrollbarState(scrollID);

			if (IsRendering)
			{
				float padding = theme.padding;
				DrawPanel(pos, size, theme.backgroundCol, anchor);
				Bounds2D bounds = PrevBounds;
				Bounds2D scrollArea = new(bounds.Min + Vector2.one * padding, bounds.Max - Vector2.one * padding + Vector2.left * theme.scrollBarWidth);

				Vector2 buttonTopLeft = scrollArea.TopLeft;
				Bounds2D contentBounds;

				Bounds2D boundsScopeStateBeforeLayoutPas = boundsScopes.IsInsideScope ? GetCurrentBoundsScope() : default;

				using (CreateMaskScopeMinMax(scrollArea.Min, scrollArea.Max))
				{
					// Run the draw content function without actually rendering to first calculate the bounds for scrolling
					contentBounds = DrawContent(false, buttonTopLeft, drawContentFunc, scrollArea);

					float maxScrollOffsetY = Mathf.Max(0, contentBounds.Height - scrollArea.Height);
					scrollState.scrollY = Mathf.Max(0, Mathf.Min(maxScrollOffsetY, scrollState.scrollY));
					DrawContent(true, buttonTopLeft + Vector2.up * scrollState.scrollY, drawContentFunc, scrollArea);
				}

				// Draw scroll bar
				Vector2 scrollbarMin = bounds.BottomRight + Vector2.left * theme.scrollBarWidth;
				Vector2 scrollbarMax = bounds.TopRight;
				Bounds2D scrollbarArea = new(scrollbarMin, scrollbarMax);

				DrawScrollbar(scrollArea, scrollbarArea, contentBounds.Height, theme, scrollID);

				// If in bounds scope, restore its state from before the layout calculations
				if (boundsScopes.TryGetCurrentScope(out BoundsScope b))
				{
					b.Min = boundsScopeStateBeforeLayoutPas.Min;
					b.Max = boundsScopeStateBeforeLayoutPas.Max;
				}
			}

			OnFinishedDrawingUIElement(CalculateCentre(pos, size, anchor), size);
			return scrollState;

			// Draw content (once for layout, once to render)
			static Bounds2D DrawContent(bool draw, Vector2 topLeft, ScrollViewDrawContentFunc drawFunc, Bounds2D scrollArea)
			{
				bool isLayoutPass = !draw;
				using (BeginBoundsScope(draw))
				{
					drawFunc.Invoke(topLeft, scrollArea.Width, isLayoutPass);
					return GetCurrentBoundsScope();
				}
			}
		}

		public static ScrollBarState DrawScrollbar(Bounds2D scrollViewArea, Bounds2D scrollbarArea, float contentHeight, ScrollViewTheme theme, UIHandle id)
		{
			ScrollBarState state = GetScrollbarState(id);

			if (IsRendering)
			{
				// Draw background
				(Vector2 centre, Vector2 size) barArea_ss = UIToScreenSpace(scrollbarArea.Centre, scrollbarArea.Size);
				Draw.Quad(barArea_ss.centre, barArea_ss.size, theme.scrollBarColBackground);

				float scrollT = contentHeight < scrollViewArea.Height ? 0 : state.scrollY / (contentHeight - scrollViewArea.Height);

				// ContentOverflow: 1 if all content fits within the scroll area. Values greater than 1 indicate how much
				// taller the area would need to be to fit the content. For example, 1.5 would mean it has be 1.5x taller.
				float contentOverflow = scrollViewArea.Height >= contentHeight ? 1 : contentHeight / scrollViewArea.Height;
				float scrollBarHeight = scrollbarArea.Height / contentOverflow;

				float scrollHandleTopAtMaxScroll = scrollbarArea.Bottom + scrollBarHeight; // y pos (top) of scroll handle if it were scrolled fully down
				float scrollHandleTop = Mathf.Lerp(scrollbarArea.Top, scrollHandleTopAtMaxScroll, scrollT);
				Color scrollBarCol = contentOverflow > 1 ? theme.scrollBarColNormal : theme.scrollBarColInactive;

				Vector2 scrollbarSize = new(scrollbarArea.Width, scrollBarHeight);
				Vector2 scrollbarCentre = CalculateCentre(new Vector2(scrollbarArea.Left, scrollHandleTop), scrollbarSize, Anchor.TopLeft);
				(Vector2 centre, Vector2 size) scrollbar_ss = UIToScreenSpace(scrollbarCentre, scrollbarSize);

				// -- Handle input --
				bool mouseOverScrollbarArea = InputHelper.MouseInBounds_ScreenSpace(barArea_ss.centre, barArea_ss.size);
				bool mouseOverScrollbar = InputHelper.MouseInBounds_ScreenSpace(scrollbar_ss.centre, scrollbar_ss.size);

				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left) || InputHelper.IsKeyDownThisFrame(KeyCode.Escape))
				{
					state.isDragging = false;
				}

				if (mouseOverScrollbarArea && contentOverflow > 1)
				{
					if (mouseOverScrollbar) scrollBarCol = theme.scrollBarColHover;
					if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
					{
						state.isDragging = true;
						state.dragScrollOffset = mouseOverScrollbar ? InputHelper.MousePos.y - scrollbar_ss.centre.y : 0;
					}
				}

				if (state.isDragging)
				{
					scrollBarCol = theme.scrollBarColPressed;
					float inputPosMin = barArea_ss.centre.y - barArea_ss.size.y / 2 + scrollbar_ss.size.y / 2;
					float inputPosMax = barArea_ss.centre.y + barArea_ss.size.y / 2 - scrollbar_ss.size.y / 2;
					state.scrollY = (1 - Mathf.InverseLerp(inputPosMin, inputPosMax, InputHelper.MousePos.y - state.dragScrollOffset)) * (contentHeight - scrollViewArea.Height);
				}
				else if (contentOverflow > 1)
				{
					const float scrollSensitivity = 1.8f;
					Bounds2D wheelScrollableBounds = Bounds2D.Grow(scrollViewArea, scrollbarArea);
					(Vector2 centre, Vector2 size) scrollableBounds_ss = UIToScreenSpace(wheelScrollableBounds.Centre, wheelScrollableBounds.Size);
					bool mouseOverScrollView = InputHelper.MouseInBounds_ScreenSpace(scrollableBounds_ss.centre, scrollableBounds_ss.size);
					if (mouseOverScrollView || mouseOverScrollbarArea)
					{
						state.scrollY += InputHelper.MouseScrollDelta.y * -scrollSensitivity;
					}
				}

				Draw.Quad(scrollbar_ss.centre, scrollbar_ss.size, scrollBarCol);
			}

			OnFinishedDrawingUIElement(scrollbarArea.Centre, scrollbarArea.Size);
			return state;
		}

		public static void DrawText(string text, FontType font, float fontSize, Vector2 pos, Anchor anchor, Color col)
		{
			if (IsRendering)
			{
				Draw.Text(font, text, fontSize * scale, UIToScreenSpace(pos), anchor, col);
			}

			TextRenderer.BoundingBox bounds = Draw.CalculateTextBounds(text.AsSpan(), font, fontSize, pos, anchor);
			OnFinishedDrawingUIElement(bounds.Centre, bounds.Size);
		}

		public static Vector2 CalculateTextSize(ReadOnlySpan<char> text, float fontSize, FontType font) => Draw.CalculateTextBoundsSize(text, fontSize, font);

		public static InputFieldState InputField(UIHandle id, InputFieldTheme theme, Vector2 pos, Vector2 size, string defaultText, Anchor anchor, float textPad, Func<string, bool> validation = null, bool forceFocus = false)
		{
			InputFieldState state = GetInputFieldState(id);

			Vector2 centre = CalculateCentre(pos, size, anchor);
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);

			if (IsRendering)
			{
				Vector2 textCentreLeft_ss = ss.centre + Vector2.right * (-ss.size.x / 2 + textPad * scale);
				Draw.Quad(ss.centre, ss.size, theme.bgCol);

				// Focus input
				bool mouseInBounds = InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);

				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					state.SetFocus(mouseInBounds);
					state.isMouseDownInBounds = mouseInBounds;

					// Set caret pos based on mouse position
					if (mouseInBounds) state.SetCursorIndex(CharIndexBeforeMouse(textCentreLeft_ss.x), InputHelper.ShiftIsHeld);
				}

				// Hold-drag left mouse to select
				if (state.focused && InputHelper.IsMouseHeld(MouseButton.Left) && state.isMouseDownInBounds)
				{
					state.SetCursorIndex(CharIndexBeforeMouse(textCentreLeft_ss.x), true);
				}

				if (forceFocus && !state.focused)
				{
					state.SetFocus(true);
				}

				// Draw focus outline and update text
				if (state.focused)
				{
					const float outlineWidth = 0.05f;
					Draw.QuadOutline(ss.centre, ss.size, outlineWidth * scale, theme.focusBorderCol);
					foreach (char c in InputHelper.InputStringThisFrame)
					{
						bool invalidChar = char.IsControl(c) || char.IsSurrogate(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.PrivateUse;
						if (invalidChar) continue;
						state.TryInsertText(c + "", validation);
					}

					// Paste from clipboard
					if (InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.V))
					{
						state.TryInsertText(InputHelper.GetClipboardContents(), validation);
					}

					if (state.text.Length > 0)
					{
						// Backspace / delete
						if (CanTrigger(ref state.backspaceTrigger, KeyCode.Backspace))
						{
							int charDeleteCount = InputHelper.CtrlIsHeld ? state.text.Length : 1; // delete all if ctrl is held
							for (int i = 0; i < charDeleteCount; i++)
							{
								state.Delete(true, validation);
							}
						}
						else if (CanTrigger(ref state.deleteTrigger, KeyCode.Delete))
						{
							state.Delete(false, validation);
						}

						// Arrow keys
						bool select = InputHelper.ShiftIsHeld;
						bool leftArrow = CanTrigger(ref state.arrowKeyTrigger, KeyCode.LeftArrow);
						bool rightArrow = CanTrigger(ref state.arrowKeyTrigger, KeyCode.RightArrow);
						bool jumpToPrevWordStart = InputHelper.CtrlIsHeld && leftArrow;
						bool jumpToNextWordEnd = InputHelper.CtrlIsHeld && rightArrow;
						bool jumpToStart = InputHelper.IsKeyDownThisFrame(KeyCode.UpArrow) || InputHelper.IsKeyDownThisFrame(KeyCode.PageUp) || InputHelper.IsKeyDownThisFrame(KeyCode.Home) || (jumpToPrevWordStart && InputHelper.AltIsHeld);
						bool jumpToEnd = InputHelper.IsKeyDownThisFrame(KeyCode.DownArrow) || InputHelper.IsKeyDownThisFrame(KeyCode.PageDown) || InputHelper.IsKeyDownThisFrame(KeyCode.End) || (jumpToNextWordEnd && InputHelper.AltIsHeld);

						if (jumpToStart) state.SetCursorIndex(0, select);
						else if (jumpToEnd) state.SetCursorIndex(state.text.Length, select);
						else if (jumpToNextWordEnd) state.SetCursorIndex(state.NextWordEndIndex(), select);
						else if (jumpToPrevWordStart) state.SetCursorIndex(state.PrevWordIndex(), select);
						else if (leftArrow) state.DecrementCursor(select);
						else if (rightArrow) state.IncrementCursor(select);

						bool copyTriggered = InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.C);
						bool cutTriggered = InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.X);

						// Copy selected text (or all text if nothing selected)
						if (copyTriggered || cutTriggered)
						{
							string copyText = state.text;
							if (state.isSelecting) copyText = state.text.AsSpan(state.SelectionMinIndex, state.SelectionMaxIndex - state.SelectionMinIndex).ToString();
							InputHelper.CopyToClipboard(copyText);

							if (cutTriggered)
							{
								if (state.isSelecting) state.Delete(true, validation);
								else state.ClearText();
							}
						}

						// Select all
						if (InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.A)) state.SelectAll();
					}
				}

				// Draw text
				using (CreateMaskScope(centre, size))
				{
					float fontSize_ss = theme.fontSize * scale;
					bool showDefaultText = string.IsNullOrEmpty(state.text) || !Application.isPlaying;
					string displayString = showDefaultText ? defaultText : state.text;

					Color textCol = showDefaultText ? theme.defaultTextCol : theme.textCol;
					Draw.Text(theme.font, displayString, fontSize_ss, textCentreLeft_ss, Anchor.TextCentreLeft, textCol);

					if (Application.isPlaying)
					{
						Vector2 boundsSizeUpToCaret = Draw.CalculateTextBoundsSize(displayString.AsSpan(0, state.cursorBeforeCharIndex), theme.fontSize, theme.font);

						// Draw selection box
						if (state.isSelecting)
						{
							Vector2 boundsSizeUpToSelect = Draw.CalculateTextBoundsSize(displayString.AsSpan(0, state.selectionStartIndex), theme.fontSize, theme.font);
							Color col = new(0.2f, 0.6f, 1, 0.5f);
							float startX = textCentreLeft_ss.x + boundsSizeUpToCaret.x * scale;
							float endX = textCentreLeft_ss.x + boundsSizeUpToSelect.x * scale;
							if (startX > endX)
							{
								(startX, endX) = (endX, startX);
							}

							Vector2 c = new((endX + startX) / 2, textCentreLeft_ss.y);
							Vector2 s = new(endX - startX, theme.fontSize * 1.2f * scale);
							Draw.Quad(c, s, col);
						}

						// Draw caret
						const float blinkDuration = 0.5f;
						if (state.focused && (int)((Time.time - state.lastInputTime) / blinkDuration) % 2 == 0)
						{
							Vector2 caretTextBoundsTest = Draw.CalculateTextBoundsSize("Mj", theme.fontSize, theme.font);
							float caretOffset = 1 * 0.075f * (state.cursorBeforeCharIndex == 0 ? -1 : 1);
							Vector2 caretPos_ss = textCentreLeft_ss + Vector2.right * ((boundsSizeUpToCaret.x + caretOffset) * scale);
							Vector2 caretSize = new(0.125f * theme.fontSize, caretTextBoundsTest.y * 1.2f);
							Draw.Quad(caretPos_ss, caretSize * scale, theme.textCol);
						}
					}
				}
			}

			OnFinishedDrawingUIElement(centre, size);
			return state;

			static bool CanTrigger(ref InputFieldState.TriggerState triggerState, KeyCode key)
			{
				if (InputHelper.IsKeyDownThisFrame(key)) triggerState.lastManualTime = Time.time;

				if (InputHelper.IsKeyDownThisFrame(key) || (InputHelper.IsKeyHeld(key) && CanAutoTrigger(triggerState)))
				{
					triggerState.lastAutoTiggerTime = Time.time;
					return true;
				}

				return false;
			}

			static bool CanAutoTrigger(InputFieldState.TriggerState triggerState)
			{
				const float autoTriggerStartDelay = 0.5f;
				const float autoTriggerRepeatDelay = 0.04f;
				bool initialDelayOver = Time.time - triggerState.lastManualTime > autoTriggerStartDelay;
				bool canRepeat = Time.time - triggerState.lastAutoTiggerTime > autoTriggerRepeatDelay;
				return initialDelayOver && canRepeat;
			}

			int CharIndexBeforeMouse(float textLeft)
			{
				//  (note: currently assumes monospaced)
				float textBoundsWidth = Draw.CalculateTextBoundsSize(state.text, theme.fontSize, theme.font).x;
				float textRight = textLeft + textBoundsWidth * scale;
				float t = Mathf.InverseLerp(textLeft, textRight, InputHelper.MousePos.x);
				return Mathf.RoundToInt(t * state.text.Length);
			}
		}

		// Reserve spot in the drawing order for a panel. Returns an ID which can be used
		// to modify its properties (position, size, colour etc) at a later point.
		public static Draw.ID ReservePanel() => Draw.ReserveQuad();

		public static void ModifyPanel(Draw.ID id, Vector2 pos, Vector2 size, Color col, Anchor anchor = Anchor.Centre)
		{
			Vector2 centre = CalculateCentre(pos, size, anchor);
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
			Draw.ModifyQuad(id, ss.centre, ss.size, col);
			OnFinishedDrawingUIElement(centre, size);
		}

		public static void ModifyPanel(Draw.ID id, Bounds2D bounds, Color col)
		{
			Vector2 centre = bounds.Centre;
			Vector2 size = bounds.Size;
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
			Draw.ModifyQuad(id, ss.centre, ss.size, col);
			OnFinishedDrawingUIElement(centre, size);
		}

		public static void DrawPanel(Bounds2D bounds, Color col)
		{
			DrawPanel(bounds.Centre, bounds.Size, col);
		}

		public static void DrawFullscreenPanel(Color col)
		{
			DrawPanel(Vector2.zero, new Vector2(Width, Height), col, Anchor.BottomLeft);
		}

		public static void DrawPanel(Vector2 pos, Vector2 size, Color col, Anchor anchor = Anchor.Centre)
		{
			Vector2 centre = CalculateCentre(pos, size, anchor);

			if (IsRendering)
			{
				(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
				Draw.Quad(ss.centre, ss.size, col);
			}

			OnFinishedDrawingUIElement(centre, size);
		}

		public static void DrawLine(Vector2 a, Vector2 b, float thickness, Color col)
		{
			if (IsRendering)
			{
				Draw.Line(UIToScreenSpace(a), UIToScreenSpace(b), thickness * scale, col);
			}
		}

		public static void DrawCircle(Vector2 pos, float radius, Color col, Anchor anchor = Anchor.Centre)
		{
			Vector2 size = Vector2.one * radius;
			Vector2 centre = CalculateCentre(pos, size, anchor);

			if (IsRendering)
			{
				(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
				Draw.Point(ss.centre, ss.size.x, col);
			}

			OnFinishedDrawingUIElement(centre, size);
		}

		public static Color DrawColourPicker(UIHandle id, Vector2 pos, float width, Anchor anchor = Anchor.Centre)
		{
			Color colRgb = Color.magenta;
			ColourPickerState state = GetColourPickerState(id);
			Vector2 centre = CalculateCentre(pos, Vector2.one * width, anchor);

			// Calculate width and spacing of hue bar
			float hueBarWidth = width / 10;
			float hueBarSpacing = hueBarWidth / 3;

			// Calculate size and centre of sat-val square
			float satValWidth = width - (hueBarWidth + hueBarSpacing);
			Vector2 satValSize = Vector2.one * satValWidth;
			Vector2 satValCentre = CalculateCentre(centre + new Vector2(-width, width) / 2, satValSize, Anchor.TopLeft);
			// Calculate hue bar position
			Vector2 hueBarSize = new(hueBarWidth, satValSize.y);
			Vector2 hueCentre = CalculateCentre(centre + new Vector2(width, width) / 2, hueBarSize, Anchor.TopRight);
			// Calculate element bounds
			Vector2 elementLeft = satValCentre + Vector2.left * satValWidth / 2;
			Vector2 elementRight = hueCentre + Vector2.right * hueBarWidth / 2;
			Vector2 elementCentre = (elementLeft + elementRight) / 2;
			Vector2 elementSize = new(elementRight.x - elementLeft.x, satValSize.y);

			if (IsRendering)
			{
				(Vector2 centre, Vector2 size) hue_ss = UIToScreenSpace(hueCentre, hueBarSize);
				(Vector2 centre, Vector2 size) satVal_ss = UIToScreenSpace(satValCentre, satValSize);

				// Draw satval and hue bar
				Draw.SatValQuad(satVal_ss.centre, satVal_ss.size, state.hue);
				Draw.HueQuad(hue_ss.centre, hue_ss.size);

				// Draw hue handle
				float hueTopY_ss = hue_ss.centre.y + hue_ss.size.y / 2;
				float hueBottomY_ss = hue_ss.centre.y - hue_ss.size.y / 2;


				float hueHandleY_ss = Mathf.Lerp(hueBottomY_ss, hueTopY_ss, state.hue);
				Vector2 hueHandlePos_ss = new(hue_ss.centre.x, hueHandleY_ss);
				Vector2 hueHandleSize_ss = new(hue_ss.size.x * 1.1f, hue_ss.size.x * 0.5f);
				Draw.Quad(hueHandlePos_ss, hueHandleSize_ss, Color.white);

				// Hue mouse input
				bool mouseOverHueBounds = InputHelper.MouseInBounds_ScreenSpace(hue_ss.centre, hue_ss.size);
				bool mouseOverHueHandle = InputHelper.MouseInBounds_ScreenSpace(hueHandlePos_ss, hueHandleSize_ss);

				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && (mouseOverHueBounds || mouseOverHueHandle))
				{
					state.hueHandleSelected = true;
				}

				if (state.hueHandleSelected)
				{
					state.hue = Remap01(hueBottomY_ss, hueTopY_ss, InputHelper.MousePos.y);
				}

				// Draw sat-val handle
				Vector2 satValBottomLeft_ss = satVal_ss.centre - satVal_ss.size / 2;
				Vector2 satValTopRight_ss = satVal_ss.centre + satVal_ss.size / 2;
				float satPos_ss = Mathf.Lerp(satValBottomLeft_ss.x, satValTopRight_ss.x, state.sat);
				float valPos_ss = Mathf.Lerp(satValBottomLeft_ss.y, satValTopRight_ss.y, state.val);


				Vector2 satValHandlePos_ss = new(satPos_ss, valPos_ss);
				float satValHandleRadius_ss = hueHandleSize_ss.y * 0.5f;
				float satValHandleRadiusOutline_ss = satValHandleRadius_ss + 0.2f * scale;
				colRgb = state.GetRGB();
				Color handleOutlineCol = ColHelper.ShouldUseBlackText(colRgb) ? Color.black : Color.white;
				Draw.Point(satValHandlePos_ss, satValHandleRadiusOutline_ss, handleOutlineCol);
				Draw.Point(satValHandlePos_ss, satValHandleRadius_ss, colRgb);

				// Sat-val mouse input
				bool mouseOverSatValBounds = InputHelper.MouseInBounds_ScreenSpace(satVal_ss.centre, satVal_ss.size);
				bool mouseOverSatValHandle = InputHelper.MouseInPoint_ScreenSpace(satValHandlePos_ss, satValHandleRadiusOutline_ss);
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left) && (mouseOverSatValBounds || mouseOverSatValHandle))
				{
					state.satValHandleSelected = true;
				}

				if (state.satValHandleSelected)
				{
					state.sat = Remap01(satValBottomLeft_ss.x, satValTopRight_ss.x, InputHelper.MousePos.x);
					state.val = Remap01(satValBottomLeft_ss.y, satValTopRight_ss.y, InputHelper.MousePos.y);
				}

				// Mouse up input
				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
				{
					state.hueHandleSelected = false;
					state.satValHandleSelected = false;
				}
			}


			OnFinishedDrawingUIElement(elementCentre, elementSize);
			return colRgb;
		}

		public static void TextWithBackground(Vector2 pos, Vector2 size, Anchor anchor, string text, FontType font, float fontSize, Color textCol, Color backgroundCol)
		{
			Vector2 centre = CalculateCentre(pos, size, anchor);
			if (IsRendering)
			{
				DrawPanel(centre, size, backgroundCol);
				DrawText(text, font, fontSize, centre, Anchor.Centre, textCol);
			}

			OnFinishedDrawingUIElement(centre, size);
		}

		public static bool Button(string text, ButtonTheme theme, Vector2 pos, bool enabled = true, Anchor anchor = Anchor.Centre) => Button(text, theme, pos, Vector2.zero, true, true, enabled, anchor);

		public static bool Button(string text, ButtonTheme theme, Vector2 pos, Vector2 size, bool enabled = true, bool fitToText = true, Anchor anchor = Anchor.Centre) => Button(text, theme, pos, size, enabled, fitToText, fitToText, anchor);

		public static bool Button(string text, ButtonTheme theme, Vector2 pos, Vector2 size, bool enabled, bool fitTextX, bool fitTextY, Anchor anchor = Anchor.Centre, bool leftAlignText = false, float textOffsetX = 0, bool ignoreInputs = false)
		{
			enabled &= !forceInteractionDisabled;

			// --- Calculate centre and size in screen space ---
			// Optionally resize button to fit text (given size is treated as padding in this case; text assumed single line)
			if (fitTextX || fitTextY)
			{
				float minSizeX = Draw.CalculateTextBoundsSize(text.AsSpan(), theme.fontSize, theme.font).x;
				float minSizeY = Draw.CalculateTextBoundsSize(MString.AsSpan(), theme.fontSize, theme.font).y;
				float padX = minSizeY * theme.paddingScale.x;
				float padY = minSizeY * theme.paddingScale.y;
				if (fitTextX) size.x += minSizeX + padX;
				if (fitTextY) size.y += minSizeY + padY;
			}

			Vector2 centre = CalculateCentre(pos, size, anchor);
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);
			bool buttonPressedThisFrame = false;

			if (IsRendering)
			{
				float fontSize_ss = theme.fontSize * scale;

				// --- Handle interaction ---
				bool mouseInsideMask = Draw.IsPointInsideActiveMask(InputHelper.MousePos) && !ignoreInputs;
				bool mouseOver = mouseInsideMask && InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);
				bool mouseIsDown = InputHelper.IsMouseHeld(MouseButton.Left);

				if (mouseOver && enabled)
				{
					buttonPressedThisFrame = InputHelper.IsMouseDownThisFrame(MouseButton.Left);
				}

				// --- Draw ---
				Color buttonCol = theme.buttonCols.GetCol(mouseOver, mouseIsDown, enabled);
				Draw.Quad(ss.centre, ss.size, buttonCol);

				Color textCol = theme.textCols.GetCol(mouseOver, mouseIsDown, enabled);
				if (!string.IsNullOrEmpty(text))
				{
					Anchor textAnchor = leftAlignText ? Anchor.TextCentreLeft : Anchor.TextFirstLineCentre;
					Vector2 textPos = leftAlignText ? ss.centre + Vector2.right * (-ss.size.x / 2 + textOffsetX * scale) : ss.centre + Vector2.right * (textOffsetX * scale);
					Draw.Text(theme.font, text, fontSize_ss, textPos, textAnchor, textCol);
				}
			}

			// Update layout and return
			OnFinishedDrawingUIElement(centre, size);
			return buttonPressedThisFrame;
		}

		public static bool MouseInsideBounds(Bounds2D uiBounds)
		{
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(uiBounds.Centre, uiBounds.Size);
			return InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);
		}

		// Returns the index of the pressed button (-1 if none)
		public static int VerticalButtonGroup(string[] names, ButtonTheme theme, Vector2 pos, Vector2 buttonSize, bool fitToTextX, bool fitToTextY, float spacing) => VerticalButtonGroup(names, null, theme, pos, buttonSize, fitToTextX, fitToTextY, spacing);

		// Returns the index of the pressed button (-1 if none)
		public static int VerticalButtonGroup(string[] names, bool[] activeStates, ButtonTheme theme, Vector2 pos, Vector2 buttonSize, bool fitToTextX, bool fitToTextY, float spacing)
		{
			int buttonPressIndex = -1;
			for (int i = 0; i < names.Length; i++)
			{
				bool active = activeStates == null ? true : activeStates[i];
				if (Button(names[i], theme, pos, buttonSize, active, fitToTextX, fitToTextY))
				{
					buttonPressIndex = i;
				}

				pos.y -= PrevBounds.Height + spacing;
			}

			return buttonPressIndex;
		}

		// Returns the index of the pressed button (-1 if none)
		public static int HorizontalButtonGroup(string[] names, ButtonTheme theme, Vector2 pos, float regionWidth, float spacing, float pad, Anchor anchor) => HorizontalButtonGroup(names, null, theme, pos, regionWidth, spacing, pad, anchor);

		public static int HorizontalButtonGroup(ReadOnlySpan<string> names, ReadOnlySpan<bool> activeStates, ButtonTheme theme, Vector2 pos, float regionWidth, float spacing, float pad, Anchor anchor, bool ignoreInputs = false) => HorizontalButtonGroup(names, activeStates, theme, pos, Vector2.right * regionWidth, spacing, pad, anchor, true, ignoreInputs);

		public static int HorizontalButtonGroup(ReadOnlySpan<string> names, ReadOnlySpan<bool> activeStates, ButtonTheme theme, Vector2 pos, Vector2 size, float spacing, float pad, Anchor anchor, bool autoHeight = false, bool ignoreInputs = false)
		{
			int buttonPressIndex = -1;
			pos += Vector2.right * pad;
			size.x -= pad * 2;
			Vector2 centre = CalculateCentre(pos, size, anchor);

			Bounds2D bounds = Bounds2D.CreateEmpty();

			Anchor buttonAnchor = anchor switch
			{
				Anchor.TopLeft => Anchor.CentreTop,
				Anchor.BottomLeft => Anchor.CentreBottom,
				_ => Anchor.Centre
			};

			for (int i = 0; i < names.Length; i++)
			{
				(Vector2 buttonSize, Vector2 buttonCentre) = HorizontalLayout(names.Length, i, centre, size, spacing);
				Vector2 buttonPos = new(buttonCentre.x, pos.y);

				bool active = activeStates == null || activeStates[i];
				if (Button(names[i], theme, buttonPos, buttonSize, active, false, autoHeight, buttonAnchor, ignoreInputs: ignoreInputs))
				{
					buttonPressIndex = i;
				}

				bounds = Bounds2D.Grow(bounds, PrevBounds);
			}

			OnFinishedDrawingUIElement(bounds.Centre, bounds.Size);
			return buttonPressIndex;
		}

		// A stateful version of Button, allowing for more complex behaviour like detecting if button was pressed and then later released
		public static ButtonState Button_Stateful(UIHandle id, string text, ButtonTheme theme, Vector2 pos, Vector2 size, bool enabled, bool fitTextX, bool fitTextY, Anchor anchor)
		{
			ButtonState state = GetButtonState(id);
			// --- Calculate centre and size in screen space ---
			// Optionally resize button to fit text (given size is treated as padding in this case; text assumed single line)
			if (fitTextX || fitTextY)
			{
				float minSizeX = Draw.CalculateTextBoundsSize(text.AsSpan(), theme.fontSize, theme.font).x;
				float minSizeY = Draw.CalculateTextBoundsSize(MString.AsSpan(), theme.fontSize, theme.font).y;
				float padX = minSizeY * theme.paddingScale.x;
				float padY = minSizeY * theme.paddingScale.y;
				if (fitTextX) size.x += minSizeX + padX;
				if (fitTextY) size.y += minSizeY + padY;
			}

			Vector2 centre = CalculateCentre(pos, size, anchor);
			(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, size);

			if (IsRendering)
			{
				float fontSize_ss = theme.fontSize * scale;

				// --- Handle interation ---
				bool mouseInsideMask = Draw.IsPointInsideActiveMask(InputHelper.MousePos);
				bool mouseOver = mouseInsideMask && InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size);
				bool mouseIsDown = InputHelper.IsMouseHeld(MouseButton.Left);
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					if (mouseOver) state.NotifyPressed();
					else state.NotifyCancelled();
				}

				if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
				{
					if (mouseOver && state.isDown) state.NotifyReleased();
					else state.NotifyCancelled();
				}

				// --- Draw ---
				Color buttonCol = theme.buttonCols.GetCol(mouseOver, mouseIsDown, enabled);
				Draw.Quad(ss.centre, ss.size, buttonCol);

				Color textCol = theme.textCols.GetCol(mouseOver, mouseIsDown, enabled);
				if (!string.IsNullOrEmpty(text))
				{
					Draw.Text(theme.font, text, fontSize_ss, ss.centre, Anchor.Centre, textCol);
				}
			}

			// Update layout and return
			OnFinishedDrawingUIElement(centre, size);
			return state;
		}

		public static void DrawToggle(UIHandle id, Vector2 pos, float size, CheckboxTheme theme, Anchor anchor = Anchor.Centre)
		{
			Vector2 boxSize = Vector2.one * size;
			Vector2 centre = CalculateCentre(pos, boxSize, anchor);

			if (IsRendering)
			{
				bool state = GetOrCreateState(id, checkBoxStates);
				(Vector2 centre, Vector2 size) ss = UIToScreenSpace(centre, boxSize);

				if (InputHelper.MouseInBounds_ScreenSpace(ss.centre, ss.size))
				{
					if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
					{
						state = !state;
						checkBoxStates[id] = state;
					}
				}

				DrawPanel(centre, boxSize, Color.red);
				DrawPanel(centre, Vector2.one * boxSize, Color.white);

				if (!state)
				{
					float thickness = ss.size.y / 15;
					float crossScale = ss.size.y * 0.3f;
					Draw.Line(ss.centre - Vector2.one * crossScale, ss.centre + Vector2.one * crossScale, thickness, theme.tickCol);
					Draw.Line(ss.centre + new Vector2(1, -1) * crossScale, ss.centre + new Vector2(-1, 1) * crossScale, thickness, theme.tickCol);
				}
			}

			OnFinishedDrawingUIElement(centre, boxSize);
		}

		// Box that displays one of multiple supplied string values. Buttons on either side allow for moving between the options.
		// Returns the index of the currently displayed element.
		public static int WheelSelector(UIHandle id, Span<string> elements, Vector2 pos, Vector2 size, WheelSelectorTheme theme, Anchor anchor = Anchor.Centre, bool allowWrapAround = true, bool enabled = true)
		{
			WheelSelectorState state = GetOrCreateState(id, wheelSelectorStates);
			state.index = WheelSelector(state.index, elements, pos, size, theme, anchor, allowWrapAround, enabled);
			return state.index;
		}

		// Box that displays one of multiple supplied string values. Buttons on either side allow for moving between the options.
		// Returns the index of the currently displayed element.
		public static int WheelSelector(int elementIndex, Span<string> elements, Vector2 pos, Vector2 size, WheelSelectorTheme theme, Anchor anchor = Anchor.Centre, bool allowWrapAround = true, bool enabled = true)
		{
			Vector2 centre = CalculateCentre(pos, size, anchor);

			if (IsRendering)
			{
				float buttonWidth = theme.buttonTheme.fontSize * 1.5f;
				Vector2 buttonSize = new(buttonWidth, size.y);
				Vector2 backgroundPanelSize = new(size.x - buttonWidth * 2, size.y);

				// Draw background panel
				float panelPad = buttonWidth;
				DrawPanel(pos, backgroundPanelSize + Vector2.right * panelPad, theme.backgroundCol, anchor);

				// Draw text
				using (CreateMaskScope(Bounds2D.CreateFromCentreAndSize(centre, backgroundPanelSize)))
				{
					DrawText(elements[elementIndex], theme.buttonTheme.font, theme.buttonTheme.fontSize, centre, Anchor.TextFirstLineCentre, enabled ? theme.textCol : theme.inactiveTextCol);
				}

				// Draw left/right buttons
				bool enabledLeft = (elementIndex > 0 || allowWrapAround) && enabled;
				bool enabledRight = (elementIndex < elements.Length - 1 || allowWrapAround) && enabled;
				Vector2 leftEdge = centre - new Vector2(size.x / 2, 0);
				Vector2 rightEdge = centre + new Vector2(size.x / 2, 0);

				int delta = 0;
				if (Button("<", theme.buttonTheme, leftEdge, buttonSize, enabledLeft, false, false, Anchor.CentreLeft)) delta--;
				if (Button(">", theme.buttonTheme, rightEdge, buttonSize, enabledRight, false, false, Anchor.CentreRight)) delta++;

				// Update state
				elementIndex = (elementIndex + delta + elements.Length) % elements.Length;
			}

			OnFinishedDrawingUIElement(centre, size);
			return elementIndex;
		}

		public static void DrawCanvasRegion(Color col)
		{
			Draw.Quad(CalculateCentre(canvasBottomLeft, canvasSize, Anchor.BottomLeft), canvasSize, col);
		}

		public static void DrawLetterboxes()
		{
			Vector2 canvasTopRight = canvasBottomLeft + canvasSize;
			Color col = Color.black;
			// ---- Left/right letterbox ----
			if (canvasBottomLeft.x > 0)
			{
				Vector2 size = new(canvasBottomLeft.x, screenSize.y);
				Draw.Quad(CalculateCentre(Vector2.zero, size, Anchor.BottomLeft), size, col);
			}

			if (canvasTopRight.x < screenSize.x)
			{
				Vector2 size = new(screenSize.x - canvasTopRight.x, screenSize.y);
				Draw.Quad(CalculateCentre(Vector2.right * canvasTopRight.x, size, Anchor.BottomLeft), size, col);
			}

			// ---- Top/bottom letterbox ----
			if (canvasBottomLeft.y > 0)
			{
				Vector2 size = new(screenSize.x, canvasBottomLeft.y);
				Draw.Quad(CalculateCentre(Vector2.zero, size, Anchor.BottomLeft), size, col);
			}

			if (canvasTopRight.y < screenSize.y)
			{
				Vector2 size = new(screenSize.x, screenSize.y - canvasTopRight.y);
				Draw.Quad(CalculateCentre(Vector2.up * canvasTopRight.y, size, Anchor.BottomLeft), size, col);
			}
		}

		//  --------------------------- Helper functions ---------------------------

		public static Vector2 ScreenToUISpace(Vector2 point) => (point - canvasBottomLeft) / scale;

		public static Bounds2D UIToScreenSpace(Bounds2D bounds) => new(UIToScreenSpace(bounds.Min), UIToScreenSpace(bounds.Max));

		public static Vector2 UIToScreenSpace(Vector2 point) => canvasBottomLeft + point * scale;

		static (Vector2 centre, Vector2 size) UIToScreenSpace(Vector2 centre, Vector2 size) => (canvasBottomLeft + centre * scale, size * scale);

		public static float CalculateSizeToFitElements(float boundsSize, float spacing, int numElements)
		{
			if (numElements <= 0) return 0;
			return (boundsSize - spacing * (numElements - 1)) / numElements;
		}

		public static string CreateColouredText(string text, Color color)
		{
			return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
		}

		// TODO: Handle rich text tags
		public static string LineBreakByCharCount(ReadOnlySpan<char> text, int maxCharsPerLine)
		{
			maxCharsPerLine = Mathf.Max(1, maxCharsPerLine);
			StringBuilder sb = new();
			int textLenPrev = text.Length;

			while (text.Length > 0)
			{
				string line = GetNextLine(ref text, maxCharsPerLine).ToString();
				sb.AppendLine(line);

				if (text.Length == textLenPrev)
				{
					throw new Exception("Failed to make progress");
				}

				textLenPrev = text.Length;
			}

			return sb.ToString();
		}


		static ReadOnlySpan<char> GetNextLine(ref ReadOnlySpan<char> text, int maxLineLength)
		{
			string word = string.Empty;
			string line = string.Empty;

			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];

				// End of word
				if (c is ' ' or '\n' || i == text.Length - 1)
				{
					if (c != ' ' && c != '\n') word += c; // add the final char

					if (word.Length > 0)
					{
						if (line.Length + word.Length <= maxLineLength)
						{
							line += word;
							word = string.Empty;
						}
						else
						{
							if (word.Length > maxLineLength) // word too long to fit on a line even by itself, so have to split it up
							{
								line += word;
								line = line.AsSpan(0, maxLineLength).ToString();
							}

							break;
						}
					}

					if (c == ' ') line += c; // add the space
					if (c == '\n')
					{
						line += ' '; // treat newline char as space (so it gets cut off from next line)
						break;
					}
				}
				else
				{
					if (line.Length >= maxLineLength) break; // non-space character, and length limit is reached, so exit
					word += c;
				}
			}

			text = text.Length > line.Length ? text.Slice(line.Length) : string.Empty.AsSpan();
			return line.AsSpan(0, Mathf.Min(line.Length, maxLineLength)); // cut off any trailing spaces that exceeded the length limit
		}

		static T GetOrCreateState<T>(UIHandle id, Dictionary<UIHandle, T> statesLookup) where T : new()
		{
			if (!statesLookup.TryGetValue(id, out T state))
			{
				state = new T();
				statesLookup.Add(id, state);
			}

			return state;
		}

		public static ColourPickerState GetColourPickerState(UIHandle id) => GetOrCreateState(id, colPickerStates);

		public static InputFieldState GetInputFieldState(UIHandle id) => GetOrCreateState(id, inputFieldStates);

		public static ButtonState GetButtonState(UIHandle id) => GetOrCreateState(id, buttonStates);

		public static ScrollBarState GetScrollbarState(UIHandle id) => GetOrCreateState(id, scrollbarStates);

		public static WheelSelectorState GetWheelSelectorState(UIHandle id) => GetOrCreateState(id, wheelSelectorStates);

		public static void ResetAllStates()
		{
			inputFieldStates.Clear();
			colPickerStates.Clear();
			buttonStates.Clear();
			scrollbarStates.Clear();
			wheelSelectorStates.Clear();
		}

		public static void OverridePreviousBounds(Bounds2D bounds)
		{
			OnFinishedDrawingUIElement(bounds);
		}

		// Update bounds, etc. once element has been drawn. Given centre/size must be in UI space (not screen-space!)
		static void OnFinishedDrawingUIElement(Vector2 centre, Vector2 size)
		{
			OnFinishedDrawingUIElement(Bounds2D.CreateFromCentreAndSize(centre, size));
		}

		// Update bounds, etc. once element has been drawn. Given bounds must be in UI space (not screen-space!)
		static void OnFinishedDrawingUIElement(Bounds2D bounds)
		{
			PrevBounds = bounds;

			if (boundsScopes.TryGetCurrentScope(out BoundsScope activeBoundsScope))
			{
				activeBoundsScope.Grow(bounds.Min, bounds.Max);
			}

			if (IsRendering && mouseOverUIFrameIndex != Time.frameCount)
			{
				if (MouseInsideBounds(bounds)) mouseOverUIFrameIndex = Time.frameCount;
			}
		}

		static float Remap01(float min, float max, float val)
		{
			if (max - min == 0) return 0.5f;
			if (val <= min) return 0;
			if (val >= max) return 1;
			return (val - min) / (max - min);
		}


		public class UIScope : IDisposable
		{
			public float aspect;
			public Vector2 canvasBottomLeft;
			public Vector2 canvasSize;
			public bool drawLetterboxes;
			public float invScale;
			public float scale;
			public Vector2 screenSize;

			public void Dispose()
			{
				if (drawLetterboxes) DrawLetterboxes();
				uiScopes.ExitScope();
			}
		}

		public class BoundsScope : IDisposable
		{
			public bool DrawUI; //
			public bool IsEmpty;
			public Vector2 Max;
			public Vector2 Min;

			public void Dispose()
			{
				boundsScopes.ExitScope();

				// Grow the parent bounds
				if (boundsScopes.TryGetCurrentScope(out BoundsScope parent))
				{
					parent.Grow(Min, Max);
				}
			}

			public Bounds2D GetBounds() => IsEmpty ? new Bounds2D(Vector2.zero, Vector2.zero) : new Bounds2D(Min, Max);

			public void Init(bool draw)
			{
				Min = Vector2.one * float.MaxValue;
				Max = Vector2.one * float.MinValue;
				IsEmpty = true;
				DrawUI = draw;
			}

			public void Grow(Vector2 min, Vector2 max)
			{
				Min = Vector2.Min(min, Min);
				Max = Vector2.Max(max, Max);
				IsEmpty = false;
			}
		}

		public class DisabledScope : IDisposable
		{
			public bool IsDisabled;
			Scope<DisabledScope> scope;


			public void Dispose()
			{
				scope.ExitScope();
			}


			public void Init(bool disabled, Scope<DisabledScope> scope)
			{
				IsDisabled = disabled;
				this.scope = scope;
			}
		}
	}
}