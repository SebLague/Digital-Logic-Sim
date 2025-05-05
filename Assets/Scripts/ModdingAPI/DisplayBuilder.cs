using System;
using UnityEngine;

namespace DLS.ModdingAPI
{
    public class DisplayBuilder
    {
        public Vector2 Position;
		public float Scale;
        public Action<Vector2, float, uint[], uint[]> DrawFunction;

        public DisplayBuilder() { }

        public DisplayBuilder SetPosition(Vector2 position)
        {
            Position = position;
            return this;
        }

        public DisplayBuilder SetScale(float scale)
        {
            Scale = scale;
            return this;
        }

        public DisplayBuilder SetDrawFunction(Action<Vector2, float, uint[], uint[]> drawFunction)
        {
            DrawFunction = drawFunction;
            return this;
        }
    }
}