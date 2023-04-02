﻿namespace DLS.Simulation.ChipImplementation
{
    using static PinState;

    public class BuiltinXNOR : BuiltinSimChip
    {
        public BuiltinXNOR(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

        protected override void ProcessInputs()
        {
            bool outputIsHigh = inputPins[0].State == inputPins[1].State;
            outputPins[0].ReceiveInput(outputIsHigh ? HIGH : LOW);
        }
    }
}