using System;
using DLS.Description;
using DLS.Simulation;
using UnityEngine;

namespace DLS.ModdingAPI
{
    public class ChipBuilder
    {
        public readonly string name;
        public Vector2 size = Vector2.one;
        public Color color = Color.white;
        public PinDescription[] inputs = null;
        public PinDescription[] outputs = null;
        public DisplayDescription[] displays = null;
        public bool hideName = false;
        public Action<SimPin[], SimPin[]> simulationFunction = null;

        public ChipBuilder(string name)
        {
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

        public ChipBuilder SetDisplays(DisplayDescription[] displays)
        {
            this.displays = displays;
            return this;
        }

        public ChipBuilder HideName(bool hide = true)
        {
            this.hideName = hide;
            return this;
        }

        public ChipBuilder SetSimulationFunction(Action<SimPin[], SimPin[]> simulationFunction)
        {
            this.simulationFunction = simulationFunction;
            return this;
        }
    }
}