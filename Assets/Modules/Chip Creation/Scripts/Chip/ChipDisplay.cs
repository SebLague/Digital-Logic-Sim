using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	// This script is placed on the chip prefab, and runs when a chip is spawned.
	// It sets up the chip graphics based on the given chip description.
	public class ChipDisplay : ChipBase
	{

		[Header("References")]
		[SerializeField] TMPro.TMP_Text nameDisplay;
		[SerializeField] MeshRenderer body;
		[SerializeField] MeshRenderer bodyOutline;
		[SerializeField] MeshRenderer highlight;
		[SerializeField] Pin pinPrefab;
		[SerializeField] BoxCollider2D interactionBounds;
		[SerializeField] Palette palette;

		[Header("Display Settings")]
		[SerializeField] bool showChipName = true;
		[SerializeField] float paddingX;
		[SerializeField] float paddingY;
		[SerializeField] float pinSpacingFactor;
		[SerializeField] float outlineWidth;
		[SerializeField] float outlineDarkenAmount;

		Color outlineCol;

		public override void Load(ChipDescription description, ChipInstanceData instanceData)
		{
			base.Load(description, instanceData);
			Create(description, new Vector2(instanceData.Points[0].X, instanceData.Points[0].Y));
		}

		public override void StartPlacing(ChipDescription chipDescription, int id)
		{
			base.StartPlacing(chipDescription, id);
			Create(chipDescription, Vector2.zero);
		}


		void Create(ChipDescription chipDescription, Vector2 position)
		{
			// Set chip display name and colour
			string displayName = FormatName(Name);
			nameDisplay.text = displayName;
			nameDisplay.fontSize *= (displayName.Length >= 6) ? 0.75f : 1;

			nameDisplay.gameObject.SetActive(showChipName);
			gameObject.name = $"Chip ({Name})";

			ColorUtility.TryParseHtmlString(chipDescription.Colour, out Color chipColour);
			body.material.color = chipColour;
			outlineCol = ColourHelper.Darken(chipColour, outlineDarkenAmount, desaturateAmount: 0f);
			bodyOutline.material.color = outlineCol;
			// Set text colour to either black or white, depending on the chip colour
			nameDisplay.color = ColourHelper.TextBlackOrWhite(chipColour);

			// Calculate chip size
			Vector2 displaySize = showChipName ? nameDisplay.GetPreferredValues() : body.transform.localScale;
			float chipSizeX = displaySize.x + (showChipName ? paddingX : 0);
			float maxPinsOnOneSide = Mathf.Max(chipDescription.InputPins.Length, chipDescription.OutputPins.Length);
			float pinSpawnLength = maxPinsOnOneSide * DisplaySettings.PinSize + Mathf.Max(0, maxPinsOnOneSide - 1) * pinSpacingFactor;
			float chipSizeY = Mathf.Max(pinSpawnLength, displaySize.y) + paddingY;

			// Set size
			Size = new Vector2(chipSizeX, chipSizeY);
			body.transform.localScale = new Vector3(Size.x, Size.y, 1);
			bodyOutline.transform.localScale = new Vector3(Size.x + outlineWidth, Size.y + outlineWidth, 1);
			interactionBounds.size = Size + Vector2.one * outlineWidth;

			// Instantiate pins
			float pinStartY = (chipSizeY - paddingY - DisplaySettings.PinSize) / 2;
			float pinEndY = (-chipSizeY + paddingY + DisplaySettings.PinSize) / 2;
			Pin[] inputPins = InstantiatePins(chipDescription.InputPins, -chipSizeX / 2, pinStartY, pinEndY, PinType.SubChipInputPin);
			Pin[] outputPins = InstantiatePins(chipDescription.OutputPins, chipSizeX / 2, pinStartY, pinEndY, PinType.SubChipOutputPin);
			SetPins(inputPins, outputPins);

			// Set up highlight display
			highlight.transform.localScale = (new Vector2(chipSizeX, chipSizeY) + Vector2.one * DisplaySettings.HighlightPadding).WithZ(1);
			highlight.gameObject.SetActive(false);

			// Mouse events
			MouseInteraction = new SebInput.MouseInteraction<ChipBase>(interactionBounds.gameObject, this);
			// Position
			transform.position = position.WithZ(RenderOrder.Chip);
		}

		public override ChipInstanceData GetInstanceData()
		{
			return new ChipInstanceData()
			{
				Name = Name,
				ID = ID,
				Points = new Point[] { new Point(transform.position.x, transform.position.y) }
			};
		}

		public override void SetHighlightState(bool highlighted)
		{
			highlight.gameObject.SetActive(highlighted);
			bodyOutline.sharedMaterial.color = (highlighted) ? Color.black : outlineCol;
		}

		public override Bounds GetBounds()
		{
			return interactionBounds.bounds;
		}

		public override void NotifyMoved()
		{
			foreach (Pin pin in AllPins)
			{
				pin.NotifyMoved();
			}
		}

		Pin[] InstantiatePins(PinDescription[] pinDescriptions, float posX, float startPosY, float endPosY, PinType type)
		{
			var ordered = OrderByYPosition(pinDescriptions);
			pinDescriptions = ordered.ToArray();


			Pin[] pinDisplays = new Pin[pinDescriptions.Length];
			for (int i = 0; i < pinDescriptions.Length; i++)
			{
				PinDescription description = pinDescriptions[i];

				float t = (pinDescriptions.Length == 1) ? 0.5f : ordered.IndexOf(description) / (pinDescriptions.Length - 1f);
				float posY = Mathf.Lerp(startPosY, endPosY, t);

				Pin pin = Instantiate(pinPrefab, parent: transform);
				pin.transform.localPosition = new Vector3(posX, posY, RenderOrder.ChipPin - RenderOrder.Chip);
				pin.SetUp(this, description, type, palette.GetTheme(description.ColourThemeName));
				pinDisplays[i] = pin;
			}
			return pinDisplays;

			List<PinDescription> OrderByYPosition(IList<PinDescription> pinDescriptions)
			{
				List<PinDescription> sortedPinDescriptions = new List<PinDescription>(pinDescriptions);
				sortedPinDescriptions.Sort((a, b) => b.PositionY.CompareTo(a.PositionY));
				return sortedPinDescriptions;
			}
		}

		string FormatName(string name)
		{
			string formattedName = "";
			int numCharsInLine = 0;

			foreach (char c in name)
			{
				if (c == ' ' && numCharsInLine >= 4)
				{
					formattedName += '\n';
					numCharsInLine = 0;
				}
				else
				{
					formattedName += c;
					numCharsInLine++;
				}
			}

			return formattedName;
		}

	}
}