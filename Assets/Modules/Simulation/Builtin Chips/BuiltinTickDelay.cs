namespace DLS.Simulation.ChipImplementation
{
	using static PinState;
	public class BuiltinTickDelay : BuiltinSimChip
	{
		PinState state = LOW;

		public BuiltinTickDelay(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

		protected override void ProcessInputs()
		{
			outputPins[0].ReceiveInput(state);
			state = inputPins[0].State;
        }
	}
}
