using Seb.Vis;
using UnityEngine;

public class TextVisTest : MonoBehaviour
{
	[Multiline] public string text;
	public FontType fontType;
	public Anchor textAlign;

	public float fontSize = 1;
	public Color col = Color.white;
}