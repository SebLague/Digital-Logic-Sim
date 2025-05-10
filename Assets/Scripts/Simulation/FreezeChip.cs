using System;
using DLS.Description;

namespace DLS.Simulation
{
    public static class FreezeChip
    {
        // Index of the freeze pin in the input pins array
        public const int FreezePinIndex = 0;
        
        // Check if a chip is frozen (freeze pin is high)
        public static bool IsChipFrozen(SimChip chip)
        {
            // can't be frozen if the chip doesn't have a freeze pin (yeah I know it doesn't work for auto freeze so I have other code)
            if (!HasFreezePin(chip))
                return false;
                
            // is pin in high state
            return PinState.FirstBitHigh(chip.InputPins[FreezePinIndex].State);
        }
        
        // func to check if this has freeze pin (really simple)
        public static bool HasFreezePin(SimChip chip)
        {
            // I forgot
            return chip.InternalState != null && 
                   chip.InternalState.Length > 0 && 
                   (chip.InternalState[0] & FreezeFlagMask) != 0;
        }
        
        // yeah I have no clue, had to ask chatgpt to write this
        private const uint FreezeFlagMask = 0x80000000; // Using the highest bit as the freeze flag
        
        // sets freeze feature (you dumb if you don't get this)
        public static void SetFreezeFeature(SimChip chip, bool enabled)
        {
            // very cool
            if (chip.InternalState == null || chip.InternalState.Length == 0)
            {
                // forgot, but very cool
                return;
            }
                
            if (enabled)
                chip.InternalState[0] |= FreezeFlagMask;
            else
                chip.InternalState[0] &= ~FreezeFlagMask;
        }
        
        // something with to do with "Ensure Internal State And Set Freeze"
        public static bool EnsureInternalStateAndSetFreeze(SimChip chip, bool enabled)
        {
            // mr. if statement
            if (chip.InternalState == null || chip.InternalState.Length == 0)
            {
                // return false to indicate failure
                return false;
            }
            
            // set freeze :chill:
            SetFreezeFeature(chip, enabled);
            return true;
        }
    }
}
