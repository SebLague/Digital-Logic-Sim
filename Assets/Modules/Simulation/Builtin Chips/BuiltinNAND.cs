﻿namespace DLS.Simulation.ChipImplementation
{
    using static PinState;

    public class BuiltinNAND : BuiltinSimChip
    {
        public BuiltinNAND(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

        protected override void ProcessInputs()
        {
            bool outputIsHigh = inputPins[0].State is LOW || inputPins[1].State is LOW;
            outputPins[0].ReceiveInput(outputIsHigh ? HIGH : LOW);
        }
    }
}
