using System.Collections.Generic;
using DLS.Simulation;

public static class PortRegistry
{
    private static int nextInputPortIndex = 0;
    private static int nextOutputPortIndex = 0;
    private static readonly Dictionary<SimChip, int> inputPortRegistry = new();
    private static readonly Dictionary<SimChip, int> outputPortRegistry = new();

    public static int RegisterInputPort(SimChip chip)
    {
        if (inputPortRegistry.TryGetValue(chip, out int existingIndex))
            return existingIndex;

        int newIndex = nextInputPortIndex++;
        inputPortRegistry.Add(chip, newIndex);
        UnityEngine.Debug.Log("Registered new input port index " + newIndex);
        
        // Initialize the chip's internal state with the port index
        if (chip.InternalState == null || chip.InternalState.Length == 0)
        {
            chip.UpdateInternalState(new uint[1] { (uint) newIndex });
        }
        else
        {
            chip.InternalState[0] = (uint)newIndex;
        }
        
        return newIndex;
    }

    public static int RegisterOutputPort(SimChip chip)
    {
        if (outputPortRegistry.TryGetValue(chip, out int existingIndex))
            return existingIndex;

        int newIndex = nextOutputPortIndex++;
        outputPortRegistry.Add(chip, newIndex);
        UnityEngine.Debug.Log("Registered new output port index " + newIndex);
        
        // Initialize the chip's internal state with the port index
        if (chip.InternalState == null || chip.InternalState.Length == 0)
        {
            chip.UpdateInternalState(new uint[1] { (uint) newIndex });
        }
        else
        {
            chip.InternalState[0] = (uint)newIndex;
        }
        
        return newIndex;
    }

    public static void UnregisterPort(SimChip chip)
    {
        inputPortRegistry.Remove(chip);
        outputPortRegistry.Remove(chip);
        // Note: Not decrementing nextIndex to keep things simple
        // Port indexes are currently never reused during runtime
    }

    public static int? GetPortIndex(SimChip chip)
    {
        if (inputPortRegistry.TryGetValue(chip, out int inputIndex))
            return inputIndex;
        if (outputPortRegistry.TryGetValue(chip, out int outputIndex))
            return outputIndex;
        return null;
    }
}