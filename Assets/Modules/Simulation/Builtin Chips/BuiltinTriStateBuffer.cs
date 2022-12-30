namespace DLS.Simulation.ChipImplementation
{
	public class BuiltinTriStateBuffer : BuiltinSimChip
	{
		public BuiltinTriStateBuffer(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

		protected override void ProcessInputs()
		{
			PinState enable = inputPins[0].State;
			PinState data = inputPins[1].State;
			PinState output = enable is PinState.HIGH ? data : PinState.FLOATING;
			outputPins[0].ReceiveInput(output);
		}
	}
}
