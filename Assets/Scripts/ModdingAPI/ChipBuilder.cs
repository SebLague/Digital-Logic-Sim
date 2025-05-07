using System;
using UnityEngine;

namespace DLS.ModdingAPI
{
    public class ChipBuilder
    {
        public readonly string modID;
        public readonly string name;
        public Vector2 size = Vector2.one;
        public Color color = Color.white;
        public PinDescription[] inputs = null;
        public PinDescription[] outputs = null;
        public DisplayBuilder[] displays = null;
        public bool hideName = false;
        public Action<uint[], uint[]> simulationFunction = null;

        public ChipBuilder(string modID, string name)
        {
            this.modID = modID;
            this.name = name;
        }

        public ChipBuilder SetSize(Vector2 size)
        {
            this.size = size;
            return this;
        }

        public ChipBuilder SetColor(Color color)
        {
            this.color = color;
            return this;
        }

        public ChipBuilder SetInputs(PinDescription[] inputs)
        {
            this.inputs = inputs;
            return this;
        }

        public ChipBuilder SetOutputs(PinDescription[] outputs)
        {
            this.outputs = outputs;
            return this;
        }

        public ChipBuilder SetDisplays(DisplayBuilder[] displays)
        {
            this.displays = displays;
            return this;
        }

        public ChipBuilder HideName(bool hide = true)
        {
            this.hideName = hide;
            return this;
        }

        public ChipBuilder SetSimulationFunction(Action<uint[], uint[]> simulationFunction)
        {
            this.simulationFunction = simulationFunction;
            return this;
        }
    }
}