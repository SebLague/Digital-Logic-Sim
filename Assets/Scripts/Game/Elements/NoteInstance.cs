using System;
using DLS.Graphics;
using Seb.Helpers;
using Seb.Types;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Game
{
    public class NoteInstance : IMoveable
    {
        // Position of the note in the simulation
        public Vector2 Position { get; set; }

        // Position when the move operation started
        public Vector2 MoveStartPosition { get; set; }

        // Reference point for straight-line movement
        public Vector2 StraightLineReferencePoint { get; set; }

        // Indicates if a reference point for straight-line movement exists
        public bool HasReferencePointForStraightLineMovement { get; set; }

        // Indicates if the note is currently selected
        public bool IsSelected { get; set; }

        // Indicates if the current position is valid for movement
        public bool IsValidMovePos { get; set; }

        // Snap point for grid alignment (can be overridden if needed)
        public virtual Vector2 SnapPoint => Position;

        // Bounding box for selection

        // Bounding box for the note
        public Bounds2D BoundingBox => Bounds2D.CreateFromCentreAndSize(Position + new Vector2(Width / 2, Height / 2), new Vector2(Width, Height));
        public virtual Bounds2D SelectionBoundingBox => Bounds2D.CreateFromCentreAndSize(Position + new Vector2(Width / 2, Height / 2), new Vector2(Width + DrawSettings.ChipOutlineWidth + DrawSettings.SelectionBoundsPadding, Height + DrawSettings.ChipOutlineWidth + DrawSettings.SelectionBoundsPadding));

        // Unique identifier for the note
        public int ID { get; private set; }

        // Text content of the note
        public string Text { get; set; }

        // Dimensions of the note
        public float Width { get; set; }
        public float Height { get; set; }

        // Constructor
        public NoteInstance(int id, Vector2 position, string text, float width = 100, float height = 50)
        {
            ID = id;
            Position = position;
            Text = text;
            Width = width;
            Height = height;
            IsSelected = false;
            IsValidMovePos = true;
        }

        // Determines if the note should be included in a selection box
        public bool ShouldBeIncludedInSelectionBox(Vector2 selectionCentre, Vector2 selectionSize)
        {
            var halfSelectionSize = selectionSize / 2;
            var halfNoteSize = new Vector2(Width, Height) / 2;

            return Math.Abs(Position.x - selectionCentre.x) <= (halfSelectionSize.x + halfNoteSize.x) &&
                Math.Abs(Position.y - selectionCentre.y) <= (halfSelectionSize.y + halfNoteSize.y);
        }
    }
}