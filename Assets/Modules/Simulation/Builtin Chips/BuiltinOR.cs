namespace DLS.Simulation.ChipImplementation
{
    using static PinState;

    public class BuiltinOR: BuiltinSimChip
    {
        public BuiltinOR(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

        protected override void ProcessInputs()
        {
            bool outputIsHigh = inputPins[0].State is HIGH || inputPins[1].State is HIGH;
            outputPins[0].ReceiveInput(outputIsHigh ? HIGH : LOW);
        }
    }
}
