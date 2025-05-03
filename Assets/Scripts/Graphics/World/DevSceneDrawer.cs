using System;
using System.Collections.Generic;
using DLS.Description;
using DLS.Game;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.Text.Rendering;
using UnityEngine;
using static DLS.Graphics.DrawSettings;
using ChipTypeHelper = DLS.Description.ChipTypeHelper;

namespace DLS.Graphics
{
	public static class DevSceneDrawer
	{
		public const int DisplayOffState = 0;
		public const int DisplayOnState = 1;
		public const int DisplayHighlightState = 2;

		static readonly List<WireInstance> orderedWires = new();
		static readonly Comparison<WireInstance> WireComparison = WireOrderCompare;

		// State
		static ChipInteractionController controller;
		static bool canEditViewedChip;

		public static void DrawActiveScene()
		{
			WorldDrawer.DrawGridIfActive(ActiveTheme.GridCol);

			controller = Project.ActiveProject.controller;
			canEditViewedChip = Project.ActiveProject.CanEditViewedChip;

			DrawWires();
			DrawWireEditPoints(controller.wireToEdit);

			// Draw dev pins and subchips (non-selected only)
			DrawMoveableElements(false);

			if (controller.IsMovingSelection) DrawAllPinNamesAndChipLabels(); // Draw labels under moving elements

			// Draw selected items on top
			Draw.StartLayer(Vector2.zero, 1, false);
			DrawMoveableElements(true);

			if (!controller.IsMovingSelection) DrawAllPinNamesAndChipLabels();

			if (InteractionState.PinUnderMouse != null && controller.HasControl && !controller.IsMovingSelection && !controller.IsCreatingSelectionBox)
			{
				DrawPin(InteractionState.PinUnderMouse);
			}

			// Draw selection box
			if (controller.IsCreatingSelectionBox)
			{
				Draw.Quad(controller.SelectionBoxCentre, controller.SelectionBoxSize, ActiveTheme.SelectionBoxCol);
			}
		}

		static void DrawWires()
		{
			DevChipInstance chip = controller.ActiveDevChip;

			// Create list of wires sorted by draw order
			orderedWires.Clear();
			orderedWires.AddRange(chip.Wires);
			if (controller.WireToPlace != null) orderedWires.Add(controller.WireToPlace);

			foreach (WireInstance wire in orderedWires)
			{
				wire.drawOrder = WireDrawOrder(wire);
			}

			orderedWires.Sort(WireComparison);

			// Draw
			foreach (WireInstance wire in orderedWires)
			{
				DrawWire(wire);
			}

			foreach (WireInstance wire in controller.DuplicatedWires)
			{
				DrawWire(wire);
			}
		}

		static void DrawAllPinNamesAndChipLabels()
		{
			DevChipInstance chip = Project.ActiveProject.controller.ActiveDevChip;
			Draw.StartLayer(Vector2.zero, 1, false);

			// -- Draw names of all pins (when mode is set to always). Also draw decimal displays for multi-bit pins --
			bool drawAllDevPinNames = Project.ActiveProject.AlwaysDrawDevPinNames;
			bool drawAllSubchipPinNames = Project.ActiveProject.AlwaysDrawSubChipPinNames;

			foreach (IMoveable element in chip.Elements)
			{
				if (element is DevPinInstance devPin)
				{
					if (drawAllDevPinNames) DrawPinLabel(devPin.Pin);
					if (devPin.BitCount != PinBitCount.Bit1) DrawPinDecValue(devPin);
				}
				else if (element is SubChipInstance subchip)
				{
					if (drawAllSubchipPinNames)
					{
						foreach (PinInstance subChipPin in subchip.AllPins)
						{
							DrawPinLabel(subChipPin);
						}
					}

					DrawSubChipLabel(subchip);
				}
			}

			// -- Draw name of pin under mouse (when mode is set to hover) --
			if (InteractionState.ElementUnderMouse is PinInstance highlightedPin && controller.CanInteractWithPin)
			{
				bool drawHighlightedPinName = false;
				drawHighlightedPinName |= highlightedPin.parent is SubChipInstance && Project.ActiveProject.description.Prefs_ChipPinNamesDisplayMode == PreferencesMenu.DisplayMode_OnHover;
				drawHighlightedPinName |= highlightedPin.parent is DevPinInstance && Project.ActiveProject.description.Prefs_MainPinNamesDisplayMode == PreferencesMenu.DisplayMode_OnHover;

				if (drawHighlightedPinName)
				{
					Draw.StartLayer(Vector2.zero, 1, false);
					DrawPinLabel(highlightedPin);
				}
			}
		}

		// Draw all moveable elements (either non-selected only, or selected only)
		static void DrawMoveableElements(bool drawSelectedOnly)
		{
			DevChipInstance chip = controller.ActiveDevChip;
			List<IMoveable> elements = drawSelectedOnly ? controller.SelectedElements : chip.Elements;
			bool allValidPlacement = true;

			foreach (IMoveable element in elements)
			{
				if (element.IsSelected == drawSelectedOnly)
				{
					switch (element)
					{
						case DevPinInstance pin:
							DrawDevPin(pin);
							break;
						case SubChipInstance subchip:
						{
							DrawSubChip(subchip);
							break;
						}
					}
				}

				allValidPlacement &= element.IsValidMovePos;
			}

			// -- Draw selection boxes and subchip displays -- (start new layer to draw above chip names)
			Draw.StartLayer(Vector2.zero, 1, false);

			foreach (IMoveable element in elements)
			{
				if (element.IsSelected == drawSelectedOnly)
				{
					// Draw displays
					if (element is SubChipInstance subchip)
					{
						// Get sim representation of this subchip (note: if the subchip has not yet been placed, this will be null)
						SimChip sim = chip.SimChip.TryGetSubChipFromID(subchip.ID).chip;
						DrawSubchipDisplays(subchip, sim);
					}

					if (element.IsSelected)
					{
						Color boxCol = ActiveTheme.SelectionBoxCol;
						if (controller.IsMovingSelection)
						{
							if (!element.IsValidMovePos) boxCol = ActiveTheme.SelectionBoxInvalidCol;
							else if (!allValidPlacement) boxCol = ActiveTheme.SelectionBoxOtherIsInvaldCol;
							else boxCol = ActiveTheme.SelectionBoxMovingCol;
						}

						Draw.Quad(element.SelectionBoundingBox.Centre, element.SelectionBoundingBox.Size, boxCol);
					}
				}
			}
		}

		public static void DrawPinLabel(PinInstance pin)
		{
			string text = pin.Name;
			if (string.IsNullOrWhiteSpace(text)) return;

			const float offsetX = PinRadius + 0.05f;
			FontType font = FontBold;

			Vector2 size = Draw.CalculateTextBoundsSize(text, FontSizePinLabel, font) + LabelBackgroundPadding;
			Vector2 centre = pin.GetWorldPos() + pin.ForwardDir * (size.x / 2 + offsetX);

			Draw.Quad(centre, size, ActiveTheme.PinLabelCol);
			Draw.Text(font, text, FontSizePinLabel, centre, Anchor.TextFirstLineCentre, Color.white);
		}

		public static void DrawSubChipLabel(SubChipInstance chip)
		{
			string text = chip.Label;
			if (string.IsNullOrWhiteSpace(text)) return;

			const float offsetY = 0.2f;
			FontType font = FontBold;

			Vector2 size = Draw.CalculateTextBoundsSize(text, FontSizePinLabel, font) + LabelBackgroundPadding;
			Vector2 centre = chip.Position + Vector2.down * (chip.Size.y / 2 + offsetY);

			Draw.Quad(centre, size, ActiveTheme.PinLabelCol);
			Draw.Text(font, text, FontSizePinLabel, centre, Anchor.TextFirstLineCentre, Color.white);
		}

		public static void DrawPinDecValue(DevPinInstance pin)
		{
			if (pin.pinValueDisplayMode == PinValueDisplayMode.Off) return;

			int charCount;

			if (pin.pinValueDisplayMode != PinValueDisplayMode.HEX)
			{
				charCount = StringHelper.CreateIntegerStringNonAlloc(pin.decimalDisplayCharBuffer, pin.GetStateDecimalDisplayValue());
			}

			else
			{
				charCount = StringHelper.CreateHexStringNonAlloc(pin.decimalDisplayCharBuffer, pin.GetStateDecimalDisplayValue());
			}

			FontType font = FontBold;
			Bounds2D parentBounds = pin.BoundingBox;
			const float offsetY = 0.225f;
			float centreX = pin.StateDisplayPosition.x;
			Vector2 labelSize = new(pin.StateGridSize.x, 0.2f);

			Vector2 labelCentre = new(centreX, parentBounds.Bottom + labelSize.y / 2 - offsetY);

			Draw.Quad(labelCentre, labelSize, new Color(0, 0, 0, 0.17f));
			Draw.Text(font, pin.decimalDisplayCharBuffer, charCount, FontSizePinLabel, labelCentre, Anchor.TextFirstLineCentre, Color.white);
		}

		static Color GetChipOutlineCol(Color chipCol) => ColHelper.GetValue_HSV(chipCol) < 0.075f ? ColHelper.Brighten(chipCol, 0.15f) : ColHelper.Darken(chipCol, 0.15f);

		static Color GetChipDisplayBorderCol(Color chipCol)
		{
			const float a = -0.13f;
			bool useBlackText = ColHelper.ShouldUseBlackText(chipCol);
			return useBlackText ? ColHelper.Darken(chipCol, a) : ColHelper.Brighten(chipCol, a);
		}

		public static void DrawSubChip(SubChipInstance subchip)
		{
			ChipDescription desc = subchip.Description;
			Color chipCol = desc.Colour;
			Vector2 pos = subchip.Position;
			bool isKeyChip = subchip.ChipType == ChipType.Key;

			if (isKeyChip)
			{
				// Key changes colour when pressed down
				if (PinState.FirstBitHigh(subchip.OutputPins[0].State)) chipCol = Color.white;
			}

			Color outlineCol = GetChipOutlineCol(chipCol);
			bool useBlackText = ColHelper.ShouldUseBlackText(chipCol);

			Color nameTextCol = useBlackText ? Color.black : Color.white;

			// Use low grey text col for chips with close to zero saturation (I think it maybe looks better, but not quite sure...)
			float sat = ColHelper.Saturation(chipCol);
			Color textColLowSat = useBlackText ? ColHelper.Darken(chipCol, 0.5f) : ColHelper.Brighten(chipCol, 0.25f);
			nameTextCol = Color.Lerp(textColLowSat, nameTextCol, Mathf.InverseLerp(0, 0.1f, sat));


			// Draw pins
			for (int i = 0; i < subchip.AllPins.Length; i++)
			{
				// Hide input pin on bus origin chips, since player connects to the bus wire instead (input pin is only used behind the scenes)
				if (i == 0 && ChipTypeHelper.IsBusOriginType(subchip.Description.ChipType)) continue;

				DrawPin(subchip.AllPins[i]);
			}


			// Draw outline and body
			Draw.Quad(pos, desc.Size + Vector2.one * ChipOutlineWidth, outlineCol);
			Draw.Quad(pos, desc.Size, chipCol);

			// Mouse over detection
			if (InputHelper.MouseInsideBounds_World(pos, desc.Size))
			{
				// If mouse is over one of this chip's pins, then prioritize keeping the pin highlighted (so interaction is not too fiddly)
				if (InteractionState.PinUnderMouse == null || InteractionState.PinUnderMouse.parent != subchip)
				{
					InteractionState.NotifyElementUnderMouse(subchip);
				}
			}

			// Draw name
			if (isKeyChip || desc.NameLocation != NameDisplayLocation.Hidden)
			{
				// Display on single line if name fits comfortably, otherwise use 'formatted' version (split across multiple lines)
				string displayName = isKeyChip ? subchip.activationKeyString : subchip.MultiLineName;
				if (Draw.CalculateTextBoundsSize(subchip.Description.Name, FontSizeChipName, FontBold).x < subchip.Size.x - PinRadius * 2.5f)
				{
					displayName = subchip.Description.Name;
				}

				bool nameCentre = desc.NameLocation == NameDisplayLocation.Centre || isKeyChip;
				Anchor textAnchor = nameCentre ? Anchor.TextCentre : Anchor.CentreTop;
				Vector2 textPos = nameCentre ? pos : pos + Vector2.up * (subchip.Size.y / 2 - GridSize / 2);

				// Draw background band behind text if placed at top (so it doesn't look out of place..)
				if (desc.NameLocation == NameDisplayLocation.Top)
				{
					Color bgBandCol = GetChipDisplayBorderCol(chipCol);
					Vector2 topLeft = pos + new Vector2(-desc.Size.x / 2, desc.Size.y / 2);
					TextRenderer.BoundingBox textBounds = Draw.CalculateTextBounds(displayName, FontBold, FontSizeChipName, textPos, textAnchor);
					float h = (topLeft.y - textBounds.Centre.y) * 2;

					Vector2 s = new(desc.Size.x, h);
					Vector2 c = topLeft + new Vector2(s.x, -s.y) / 2;
					Draw.Quad(c, s, bgBandCol);
				}

				Draw.Text(FontBold, displayName, FontSizeChipName, textPos, textAnchor, nameTextCol, ChipNameLineSpacing);
			}
		}

		public static void DrawSubchipDisplays(SubChipInstance subchip, SimChip sim = null, bool outOfBoundsDisplay = false)
		{
			Bounds2D subchipMask = Bounds2D.CreateFromCentreAndSize(subchip.Position, subchip.Size);

			Span<Bounds2D> allBounds = outOfBoundsDisplay ? stackalloc Bounds2D[subchip.Displays.Count] : null;

			using (Draw.BeginMaskScope(subchipMask.Min, subchipMask.Max))
			{
				// Draw displays
				for (int i = 0; i < subchip.Displays.Count; i++)
				{
					DisplayInstance display = subchip.Displays[i];
					Bounds2D bounds = DrawDisplayWithBackground(display, subchip.Position, subchip, sim);
					if (outOfBoundsDisplay) allBounds[i] = bounds;
				}
			}

			if (outOfBoundsDisplay)
			{
				Color outOfBoundsCol = new(1, 0, 0, 0.24f);
				Bounds2D maskBounds = Bounds2D.CreateFromCentreAndSize(subchip.Position, subchip.Size);

				foreach (Bounds2D bounds in allBounds)
				{
					if (bounds.EntirelyInside(maskBounds)) continue;
					Draw.Quad(bounds.Centre, bounds.Size, outOfBoundsCol);
				}
			}
		}

		public static Bounds2D DrawDisplayWithBackground(DisplayInstance display, Vector2 pos, SubChipInstance rootChip, SimChip sim = null)
		{
			Color borderCol = GetChipDisplayBorderCol(rootChip.Description.Colour);

			Draw.ID displayBorderID = Draw.ReserveQuad();
			Draw.ID displayBackingID = Draw.ReserveQuad();


			Bounds2D bounds = DrawDisplay(display, pos, 1, rootChip, sim);

			// Border colour around display
			Draw.ModifyQuad(displayBorderID, bounds.Centre, bounds.Size + Vector2.one * 0.03f, borderCol);
			// Black background behind display to fill any gaps
			Draw.ModifyQuad(displayBackingID, bounds.Centre, bounds.Size, Color.black);

			return bounds;
		}

		public static Bounds2D DrawDisplay(DisplayInstance display, Vector2 posParent, float parentScale, SubChipInstance rootChip, SimChip sim = null)
		{
			Bounds2D bounds = Bounds2D.CreateEmpty();

			Vector2 posLocal = display.Desc.Position;
			Vector2 posWorld = posParent + posLocal * parentScale;
			float scaleWorld = display.Desc.Scale * parentScale;

			if (display.DisplayType is ChipType.Custom)
			{
				sim = sim?.GetSubChipFromID(display.Desc.SubChipID);

				foreach (DisplayInstance child in display.ChildDisplays)
				{
					Bounds2D childBounds = DrawDisplay(child, posWorld, scaleWorld, rootChip, sim);
					bounds = Bounds2D.Grow(bounds, childBounds);
				}
			}
			else if (display.DisplayType is ChipType.SevenSegmentDisplay)
			{
				bool simActive = sim != null;
				// if this is the builtin 7Seg, highlight segments when mouse is over corresponding pin
				bool hoverActive = rootChip.ChipType == ChipType.SevenSegmentDisplay;
				PinInstance pin = InteractionState.PinUnderMouse;
				int colOffset = simActive && sim.InputPins[7].FirstBitHigh ? 3 : 0;

				int A = (simActive && sim.InputPins[0].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[0] ? DisplayHighlightState : DisplayOffState) + colOffset;
				int B = (simActive && sim.InputPins[1].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[1] ? DisplayHighlightState : DisplayOffState) + colOffset;
				int C = (simActive && sim.InputPins[2].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[2] ? DisplayHighlightState : DisplayOffState) + colOffset;
				int D = (simActive && sim.InputPins[3].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[3] ? DisplayHighlightState : DisplayOffState) + colOffset;
				int E = (simActive && sim.InputPins[4].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[4] ? DisplayHighlightState : DisplayOffState) + colOffset;
				int F = (simActive && sim.InputPins[5].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[5] ? DisplayHighlightState : DisplayOffState) + colOffset;
				int G = (simActive && sim.InputPins[6].FirstBitHigh ? DisplayOnState : hoverActive && pin == rootChip.AllPins[6] ? DisplayHighlightState : DisplayOffState) + colOffset;
				bounds = DrawDisplay_SevenSegment(posWorld, scaleWorld, A, B, C, D, E, F, G);
			}
			else if (display.DisplayType == ChipType.DisplayRGB)
			{
				bounds = DrawDisplay_RGB(posWorld, scaleWorld, sim);
			}
			else if (display.DisplayType == ChipType.DisplayDot)
			{
				bounds = DrawDisplay_Dot(posWorld, scaleWorld, sim);
			}
			else if (display.DisplayType == ChipType.DisplayLED)
			{
				bool simActive = sim != null;
				Color col = Color.black;
				if (simActive)
				{
					bool isOn = sim.InputPins[0].FirstBitHigh;
					uint displayColIndex = sim.InternalState[0];
					col = GetStateColour(isOn, displayColIndex);
				}

				bounds = DrawDisplay_LED(posWorld, scaleWorld, col);
			}

			display.LastDrawBounds = bounds;
			return bounds;
		}


		public static Vector2 CalculateChipNameBounds(string name) => Draw.CalculateTextBoundsSize(name, FontSizeChipName, FontBold, ChipNameLineSpacing);

		public static Bounds2D DrawDisplay_RGB(Vector2 centre, float scale, SimChip simSource)
		{
			const int pixelsPerRow = 16;
			const float borderFrac = 0.95f;
			const float pixelSizeT = 0.925f;
			// Draw background
			Draw.Quad(centre, Vector2.one * scale, Color.black);
			float size = scale * borderFrac;

			bool useSim = simSource != null;

			Vector2 bottomLeft = centre - Vector2.one * size / 2;
			float pixelSize = size / pixelsPerRow;
			Vector2 pixelDrawSize = Vector2.one * (pixelSize * pixelSizeT);
			Color col = ColHelper.MakeCol(0.1f);

			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					if (useSim)
					{
						int address = y * 16 + x;
						uint pixelState = simSource.InternalState[address];
						float red = Unpack4BitColChannel(pixelState);
						float green = Unpack4BitColChannel(pixelState >> 4);
						float blue = Unpack4BitColChannel(pixelState >> 8);
						col = new Color(red, green, blue);
					}

					Vector2 pos = bottomLeft + Vector2.one * pixelSize / 2 + Vector2.right * (pixelSize * x) + Vector2.up * (pixelSize * y);
					Draw.Quad(pos, pixelDrawSize, col);
				}
			}

			return Bounds2D.CreateFromCentreAndSize(centre, Vector2.one * scale);

			float Unpack4BitColChannel(uint raw)
			{
				return (raw & 0b1111) / 15f;
			}
		}

		public static Bounds2D DrawDisplay_Dot(Vector2 centre, float scale, SimChip simSource)
		{
			const int pixelsPerRow = 16;
			const float borderFrac = 0.95f;
			const float pixelSizeT = 0.925f;
			// Draw background
			Draw.Quad(centre, Vector2.one * scale, Color.black);
			float size = scale * borderFrac;

			bool useSim = simSource != null;

			Vector2 bottomLeft = centre - Vector2.one * size / 2;
			float pixelSize = size / pixelsPerRow;
			Vector2 pixelDrawSize = Vector2.one * (pixelSize * pixelSizeT);

			Color col = ColHelper.MakeCol(0.1f);

			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					if (useSim)
					{
						int address = y * 16 + x;
						uint pixelState = simSource.InternalState[address];
						float v = pixelState;
						col = new Color(pixelState, pixelState, pixelState);
					}

					Vector2 pos = bottomLeft + Vector2.one * pixelSize / 2 + Vector2.right * (pixelSize * x) + Vector2.up * (pixelSize * y);
					Draw.Point(pos, pixelDrawSize.x / 2, col);
				}
			}

			return Bounds2D.CreateFromCentreAndSize(centre, Vector2.one * scale);
		}

		public static Bounds2D DrawDisplay_SevenSegment(Vector2 centre, float scale, int A, int B, int C, int D, int E, int F, int G)
		{
			const float targetHeightAspect = 1.75f;
			const float segmentThicknessFac = 0.165f;
			const float segmentVerticalSpacingFac = 0.07f;
			const float displayInsetFac = 0.2f;

			float boundsWidth = scale;
			float boundsHeight = boundsWidth * targetHeightAspect;
			float segmentThickness = scale * segmentThicknessFac;

			// Width of horizontal segments
			float segmentWidth = boundsWidth - segmentThickness - scale * displayInsetFac;
			// Distance between the centres of the bottom-most and top-most segments
			float segmentRegionHeight = boundsHeight - segmentThickness - scale * displayInsetFac;
			// Height of the vertical segments
			float segmentHeight = segmentRegionHeight / 2 - scale * segmentVerticalSpacingFac;

			Vector2 segmentSizeVertical = new(segmentThickness, segmentHeight);
			Vector2 segmentSizeHorizontal = new(segmentWidth, segmentThickness);
			Vector2 offsetX = Vector2.right * segmentWidth / 2;
			Vector2 offsetY = Vector2.up * segmentRegionHeight / 4;

			Color[] cols = ActiveTheme.SevenSegCols;

			// Draw bounds
			Vector2 boundsSize = new(boundsWidth, boundsHeight);
			Draw.Quad(centre, boundsSize, Color.black);

			// Draw horizontal segments
			Draw.Diamond(centre, new Vector2(segmentWidth, segmentThickness), cols[G]); // mid
			Draw.Diamond(centre + Vector2.up * segmentRegionHeight / 2, segmentSizeHorizontal, cols[A]); // top
			Draw.Diamond(centre - Vector2.up * segmentRegionHeight / 2, segmentSizeHorizontal, cols[D]); // bottom

			// Draw vertical segments
			Draw.Diamond(centre - offsetX + offsetY, segmentSizeVertical, cols[F]); // left top
			Draw.Diamond(centre - offsetX - offsetY, segmentSizeVertical, cols[E]); // left bottom
			Draw.Diamond(centre + offsetX + offsetY, segmentSizeVertical, cols[B]); // right top
			Draw.Diamond(centre + offsetX - offsetY, segmentSizeVertical, cols[C]); // right bottom

			return Bounds2D.CreateFromCentreAndSize(centre, boundsSize);
		}

		public static Bounds2D DrawDisplay_LED(Vector2 centre, float scale, Color col)
		{
			const float pixelSizeT = 0.975f;
			Vector2 pixelDrawSize = Vector2.one * (scale * pixelSizeT);
			
			Draw.Quad(centre, Vector2.one * scale, Color.black);
			Draw.Quad(centre, pixelDrawSize, col);
			
			return Bounds2D.CreateFromCentreAndSize(centre, Vector2.one * scale);
		}

		public static void DrawDevPin(DevPinInstance devPin)
		{
			if (devPin.BitCount == PinBitCount.Bit1)
			{
				Draw1BitDevPin(devPin);
			}
			else
			{
				DrawMultiBitDevPin(devPin);
			}
		}

		public static void Draw1BitDevPin(DevPinInstance devPin)
		{
			// Line between state display and pin
			Draw.Line(devPin.StateDisplayPosition, devPin.PinPosition, 0.03f, Color.black);

			// ---- State indicator circle ----
			// Toggle state on mouse down
			bool mouseOverStateIndicator = devPin.PointIsInStateIndicatorBounds(InputHelper.MousePosWorld);
			bool interactingWithStateDisplay = mouseOverStateIndicator && devPin.IsInputPin && controller.CanInteractWithPinStateDisplay;
			Color stateCol = devPin.Pin.GetStateCol(0, interactingWithStateDisplay, canEditViewedChip);

			// Highlight on hover and toggle on mouse down
			if (interactingWithStateDisplay)
			{
				InteractionState.NotifyUnspecifiedElementUnderMouse();
				if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
				{
					controller.ToggleDevPinState(devPin, 0);
				}
			}

			Draw.Point(devPin.StateDisplayPosition, DevPinStateDisplayRadius + DevPinStateDisplayOutline, ColHelper.Darken(stateCol, 1.25f));
			Draw.Point(devPin.StateDisplayPosition, DevPinStateDisplayRadius, stateCol);

			// Draw pin and handle
			DrawPin(devPin.Pin);
			DrawPinHandle(devPin, devPin.HandlePosition, devPin.GetHandleSize());
		}

		public static void DrawMultiBitDevPin(DevPinInstance devPin)
		{
			// Line between state display and pin
			Draw.Line(devPin.StateDisplayPosition, devPin.PinPosition, 0.03f, Color.black);

			// ---- State indicator grid ----
			Vector2Int stateGridDim = devPin.StateGridDimensions;

			const float squareDisplayScaleT = 0.9f;
			Vector2 squareDisplaySize = Vector2.one * (MultiBitPinStateDisplaySquareSize * squareDisplayScaleT);
			Vector2 inputGridSize = devPin.StateGridSize;
			Vector2 inputGridSizeWithoutOutline = inputGridSize - Vector2.one * DevPinStateDisplayOutline;
			Vector2 centre = devPin.StateDisplayPosition;

			Vector2 topLeft = new(centre.x - inputGridSizeWithoutOutline.x / 2, centre.y + inputGridSizeWithoutOutline.y / 2);
			Draw.Quad(centre, inputGridSize, Color.black);
			int currBitIndex = (int)devPin.BitCount - 1;

			bool mouseOverStateGrid = InputHelper.MouseInsideBounds_World(centre, inputGridSize);
			bool isInteractable = controller.CanInteractWithPinStateDisplay && devPin.IsInputPin;

			// If mouse over state grid, register it so that player can't draw selection box here (annoying when trying to toggle states)
			// (individual toggles are tested for mouse input below, but this is a catch-all for when mouse is in gap in between)
			if (mouseOverStateGrid && isInteractable) InteractionState.NotifyUnspecifiedElementUnderMouse();

			for (int y = 0; y < stateGridDim.y; y++)
			{
				for (int x = 0; x < stateGridDim.x; x++)
				{
					Vector2 pos = topLeft + MultiBitPinStateDisplaySquareSize * new Vector2(x + 0.5f, -(y + 0.5f));

					// Highlight on hover, toggle on press
					bool mouseOverStateToggle = InputHelper.MouseInsideBounds_World(pos, squareDisplaySize);
					bool isInteractingWithStateDisplay = mouseOverStateToggle && isInteractable;
					Color stateCol = devPin.Pin.GetStateCol(currBitIndex, isInteractingWithStateDisplay, canEditViewedChip);

					if (isInteractingWithStateDisplay)
					{
						InteractionState.NotifyUnspecifiedElementUnderMouse();

						if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
						{
							controller.ToggleDevPinState(devPin, currBitIndex);
						}
					}

					Draw.Quad(pos, squareDisplaySize, stateCol);
					currBitIndex--;
				}
			}

			// Draw pin and handle
			DrawPin(devPin.Pin);
			DrawPinHandle(devPin, devPin.HandlePosition, devPin.GetHandleSize());
		}


		static void DrawPinHandle(IInteractable item, Vector2 pos, Vector2 size)
		{
			// ---- Movement handle ----
			bool mouseOverHandle = InputHelper.MouseInsideBounds_World(pos, size);
			bool isInteracting = mouseOverHandle && (controller.CanInteractWithPinHandle || InteractionState.ElementUnderMousePrevFrame == item);
			Color handleCol = isInteracting ? ActiveTheme.DevPinHandleHighlighted : ActiveTheme.DevPinHandle;
			Draw.Quad(pos, size, handleCol);

			if (isInteracting)
			{
				InteractionState.NotifyElementUnderMouse(item);
			}
		}

		public static void DrawWire(WireInstance wire)
		{
			if (wire.bitCount == PinBitCount.Bit1)
			{
				DrawSingleBitWire(wire);
			}
			else
			{
				DrawMultiBitWire(wire);
			}
		}

		// Wire should be highlighted if mouse is over it or if in edit mode
		static bool ShouldHighlightWire(WireInstance wire)
		{
			if (InteractionState.MouseIsOverUI) return false;
			if (wire == controller.wireToEdit) return true;

			if (InteractionState.ElementUnderMousePrevFrame is WireInstance wireUnderMouse && wire == wireUnderMouse)
			{
				if (controller.IsCreatingWire)
				{
					return controller.CanCompleteWireConnection(wire, out PinInstance _);
				}

				return true;
			}

			return false;
		}

		static void DrawSingleBitWire(WireInstance wire)
		{
			bool highlightWire = ShouldHighlightWire(wire);
			float thickness = highlightWire ? WireHighlightedThickness : WireThickness;

			Vector2 mousePos = InputHelper.MousePosWorld;
			const float highlightDstThreshold = WireHighlightedThickness + (WireHighlightedThickness - WireThickness) * 0.8f;
			const float sqrDstThreshold = highlightDstThreshold * highlightDstThreshold;
			bool canInteract = controller.CanInteractWithWire(wire);

			// Init and populate points array
			if (wire.BitWires[0].Points.Length != wire.WirePointCount)
			{
				wire.BitWires[0].Points = new Vector2[wire.WirePointCount];
			}

			for (int i = 0; i < wire.WirePointCount; i++)
			{
				wire.BitWires[0].Points[i] = wire.GetWirePoint(i);
			}


			// Draw
			Color col = wire.GetColour(0);
			float interactSqrDst = WireDrawer.DrawWireStraight(wire.BitWires[0].Points, thickness, col, mousePos);

			// Draw connection point (if connects to wire)
			if (wire.ConnectedWire != null)
			{
				Vector2 connectionPoint = wire.SourceConnectionInfo.IsConnectedAtWire ? wire.BitWires[0].Points[0] : wire.BitWires[0].Points[^1];

				float radius = highlightWire ? 0.07f : 0.06f;
				Draw.Point(connectionPoint, radius, col);
			}

			if (canInteract && interactSqrDst < sqrDstThreshold)
			{
				InteractionState.NotifyElementUnderMouse(wire);
			}
		}

		static void DrawMultiBitWire(WireInstance wire)
		{
			float thickness = ShouldHighlightWire(wire) ? WireHighlightedThickness : WireThickness;

			Vector2 mousePos = InputHelper.MousePosWorld;
			const float highlightDstThreshold = WireHighlightedThickness + (WireHighlightedThickness - WireThickness) * 0.8f;
			const float sqrDstThreshold = highlightDstThreshold * highlightDstThreshold;
			bool canInteract = controller.CanInteractWithWire(wire);

			WireLayoutHelper.CreateMultiBitWireLayout(wire.BitWires, wire, WireThickness);

			// Draw
			for (int bitIndex = 0; bitIndex < wire.BitWires.Length; bitIndex++)
			{
				WireInstance.BitWire bitWire = wire.BitWires[bitIndex];
				Color col = wire.GetColour(bitIndex);
				float sqrInteractDst = WireDrawer.DrawWireStraight(bitWire.Points, thickness, col, mousePos);
				if (canInteract && sqrInteractDst < sqrDstThreshold) InteractionState.NotifyElementUnderMouse(wire);
			}
		}

		static void DrawWireEditPoints(WireInstance wire)
		{
			// Wire edit points
			if (wire == null) return;
			if (!controller.isMovingWireEditPoint) controller.wireEditPointIndex = -1;
			controller.wireEditCanInsertPoint = false;
			bool canInteract = controller.CanInteractWithWire(wire);

			// Can't edit first and last point in wire (unless that point connects to another wire instead of a pin)
			int startIndex = wire.SourceConnectionInfo.IsConnectedAtWire ? 0 : 1;
			int endIndex = wire.TargetConnectionInfo.IsConnectedAtWire ? wire.WirePointCount - 1 : wire.WirePointCount - 2;

			const float r = 0.07f;
			const float rBG = r + 0.02f;

			for (int i = startIndex; i <= endIndex; i++)
			{
				Vector2 p = wire.GetWirePoint(i);
				// Mouse over (but ignore if already moving another point)
				bool highlighted = (InputHelper.MousePosWorld - p).sqrMagnitude < rBG * rBG && !controller.isMovingWireEditPoint;
				// Currently moving this point (mouse may not be over due to snapping, etc)
				highlighted |= controller.wireToEdit != null && controller.wireEditPointIndex == i;
				highlighted &= canInteract;

				Color editPointCol = highlighted ? wire.SourcePin.GetColHigh() : wire.SourcePin.GetColLow();

				Draw.Point(p, rBG, Color.white);
				Draw.Point(p, r, editPointCol);

				if (highlighted)
				{
					InteractionState.NotifyUnspecifiedElementUnderMouse();
					controller.wireEditPointIndex = i;
				}
			}

			// If no highlighted point, and mouse over wire, then draw insertion point
			if (controller.wireEditPointIndex == -1 && InteractionState.ElementUnderMouse == wire && canInteract)
			{
				const float insertionPointDisplayRadius = 0.04f;
				(Vector2 insertionPoint, int segmentIndex) = WireLayoutHelper.GetClosestPointOnWire(wire, InputHelper.MousePosWorld);
				float dstFromExistingPoint = Mathf.Min((insertionPoint - wire.GetWirePoint(segmentIndex)).magnitude, (insertionPoint - wire.GetWirePoint(segmentIndex + 1)).magnitude);
				if (dstFromExistingPoint > rBG)
				{
					controller.wireEditCanInsertPoint = true;
					Draw.Point(insertionPoint, insertionPointDisplayRadius, Color.white);
				}
			}
		}

		static void DrawPin(PinInstance pin)
		{
			if (pin.bitCount == PinBitCount.Bit1)
			{
				DrawSingleBitPin(pin);
			}
			else
			{
				DrawMultiBitPin(pin);
			}
		}

		static void DrawSingleBitPin(PinInstance pin)
		{
			Vector2 pinPos = pin.GetWorldPos();
			Vector2 pinSelectionBoundsPos = pinPos + pin.ForwardDir * 0.02f;
			float pinSelectionBoundsRadius = PinRadius + 0.015f;

			bool mouseOverPin = !InteractionState.MouseIsOverUI && InputHelper.MouseInsidePoint_World(pinSelectionBoundsPos, pinSelectionBoundsRadius);

			if (mouseOverPin) InteractionState.NotifyElementUnderMouse(pin);
			bool canInteract = controller.CanInteractWithPin;

			Color pinCol = mouseOverPin && canInteract ? ActiveTheme.PinHighlightCol : ActiveTheme.PinCol;
			// If hovering over pin while creating a wire, colour should indicate whether it is a valid connection
			if (mouseOverPin && canInteract && controller.IsCreatingWire && !controller.CanCompleteWireConnection(pin))
			{
				pinCol = ActiveTheme.PinInvalidCol;
			}

			Draw.Point(pinPos, PinRadius, pinCol);
		}

		static void DrawMultiBitPin(PinInstance pin)
		{
			Vector2 pinPos = pin.GetWorldPos();
			Vector2 pinSelectionBoundsPos = pinPos + Vector2.right * ((pin.IsSourcePin ? 1 : -1) * 0.02f);
			const float pinWidth = PinRadius * 2 * 0.95f;
			float pinHeight = SubChipInstance.PinHeightFromBitCount(pin.bitCount);
			Vector2 pinSize = new(pinWidth, pinHeight);

			bool mouseOverPin = !InteractionState.MouseIsOverUI && InputHelper.MouseInsideBounds_World(pinSelectionBoundsPos, pinSize);

			if (mouseOverPin) InteractionState.NotifyElementUnderMouse(pin);
			bool canInteract = controller.CanInteractWithPin;

			Color pinCol = mouseOverPin && canInteract ? ActiveTheme.PinHighlightCol : ActiveTheme.PinCol;
			// If hovering over pin while creating a wire, colour should indicate whether it is a valid connection
			if (mouseOverPin && canInteract && controller.IsCreatingWire && !controller.CanCompleteWireConnection(pin))
			{
				pinCol = ActiveTheme.PinInvalidCol;
			}

			Draw.Quad(pinPos, pinSize, pinCol);
		}

		public static void DrawGrid(Color gridCol)
		{
			float thickness = GridThickness;

			Camera cam = InputHelper.WorldCam;
			float screenHalfHeight = cam.orthographicSize;
			float screenHalfWidth = cam.orthographicSize * cam.aspect;
			Vector2 worldCentre = cam.transform.position;

			float left = ToGrid(-screenHalfWidth + worldCentre.x, GridSize) - GridSize;
			float right = ToGrid(screenHalfWidth + worldCentre.x, GridSize) + GridSize;
			float top = ToGrid(screenHalfHeight + worldCentre.y, GridSize) + GridSize;
			float bottom = ToGrid(-screenHalfHeight + worldCentre.y, GridSize) - GridSize;

			int skip = cam.orthographicSize < 8 ? 1 : cam.orthographicSize < 32 ? 4 : 16;

			for (float px = left; px < right; px += GridSize)
			{
				int xInt = Mathf.RoundToInt(px / GridSize);
				if (xInt % skip == 0)
				{
					Draw.Line(new Vector2(px, bottom), new Vector2(px, top), thickness, gridCol);
				}
			}

			for (float py = bottom; py < top; py += GridSize)
			{
				int yInt = Mathf.RoundToInt(py / GridSize);
				if (yInt % skip == 0)
				{
					Draw.Line(new Vector2(left, py), new Vector2(right, py), thickness, gridCol);
				}
			}

			return;

			static float ToGrid(float v, float gridSize)
			{
				int intV = (int)(v / gridSize);
				return intV * gridSize;
			}
		}

		static int WireOrderCompare(WireInstance a, WireInstance b) => a.drawOrder.CompareTo(b.drawOrder);

		static int WireDrawOrder(WireInstance wire)
		{
			// Wire in placement mode should be drawn on top of all other wires
			if (!wire.IsFullyConnected) return int.MaxValue;

			if (wire == controller.wireToEdit) return int.MaxValue - 1;

			// Bus wires should be drawn on top of regular wires
			if (wire.IsBusWire) return int.MaxValue - 2;

			// Draw wires carrying high signal above those carrying low signal (for single bit wires)
			bool wireIsHigh = wire.bitCount == PinBitCount.Bit1 && PinState.FirstBitHigh(wire.SourcePin.State);
			int drawPriority_signalHigh = wireIsHigh ? 1000 : 0;

			// Draw multi-bit wires above single bit wires
			int drawPriority_bitCount = (int)wire.bitCount * 1000;

			// If a wire is connected to another wire, it should be drawn beneath it
			// (mainly important for multi-bit wires, since these look strange otherwise)
			int drawPriority_childWire = -wire.ConnectedWireRecursionDepth;

			return (drawPriority_childWire + drawPriority_signalHigh + drawPriority_bitCount) * 100 + wire.spawnOrder;
		}
	}
}