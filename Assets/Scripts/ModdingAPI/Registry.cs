using System;
using DLS.Description;
using DLS.Game;
using DLS.Simulation;
using UnityEngine;

public static class Registry
{
    public static void RegisterChip(
        string name,
        Vector2 size,
        Color col,
        PinDescription[] inputs = null,
        PinDescription[] outputs = null,
        DisplayDescription[] displays = null,
        bool hideName = false,
        Action<SimPin[], SimPin[]> simulationFunction = null)
    {
        ModdedChipCreator.RegisterChip(name, size, col, inputs, outputs, displays, hideName, simulationFunction);
    }
}