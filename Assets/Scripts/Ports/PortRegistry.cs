using System.Collections.Generic;
using DLS.Description;
using DLS.Simulation;

public static class PortRegistry
{
    private static readonly SortedSet<int> availableInputIndices = new();
    private static readonly SortedSet<int> availableOutputIndices = new();

    public static void Initialize()
    {
        for (int i = 0; i <= 8; i++)
        {
            availableInputIndices.Add(i);
            availableOutputIndices.Add(i);
        }
    }

    public static uint RegisterInputPort()
    {
        if (availableInputIndices.Count == 0)
            return 9;
        
        int index = availableInputIndices.Min;
        availableInputIndices.Remove(index);
        
        return (uint)index;
    }

    public static uint RegisterOutputPort()
    {
        if (availableOutputIndices.Count == 0)
            return 9;
        
        int index = availableOutputIndices.Min;
        availableOutputIndices.Remove(index);
        
        return (uint)index;
    }

    public static void UnregisterPort(SimChip chip)
    {
        
        if (chip.InternalState == null || chip.InternalState.Length == 0)
        {
            return;
        }

        int index = (int)chip.InternalState[0];

        if (chip.ChipType == ChipType.Port_In)
        {
            availableInputIndices.Add(index);
        }
        else if (chip.ChipType == ChipType.Port_Out)
        {
            availableOutputIndices.Add(index);
        }
    }

    public static int? GetPortIndex(SimChip chip)
    {
        if (chip.InternalState != null && chip.InternalState.Length > 0)
            return (int)chip.InternalState[0];
        return null;
    }
}