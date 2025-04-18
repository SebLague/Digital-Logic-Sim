using UnityEngine;

namespace DLS.Description
{
    public class NoteDescription
    {
        public Vector2 Size;
        public NoteColour Colour;
        public string Text; // Text content of the note
        public Vector2 Position;
        public int ID;

        public NoteDescription(int id, Vector2 size, NoteColour colour, string text, Vector2 position)
        {
            ID = id;
            Size = size;
            Colour = colour;
            Text = text;
            Position = position;
        }
    }

    public enum NoteColour
	{
		Red,
		Yellow,
		Green,
		Blue,
		Violet,
		Pink,
		White
	}
}