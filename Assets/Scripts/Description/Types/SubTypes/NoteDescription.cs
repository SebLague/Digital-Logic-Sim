using UnityEngine;

namespace DLS.Description
{
    public class NoteDescription
    {
        public string Name;
        public Vector2 Size;
        public Color Colour;
        public string Content; // Text content of the note
        public Vector2 Position;

        public NoteDescription(string name, Vector2 size, Color colour, string content, Vector2 position)
        {
            Name = name;
            Size = size;
            Colour = colour;
            Content = content;
            Position = position;
        }
    }
}