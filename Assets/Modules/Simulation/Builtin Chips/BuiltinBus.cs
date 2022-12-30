namespace DLS.Simulation.ChipImplementation
{
	public class BuiltinBus : BuiltinSimChip
	{
		public BuiltinBus(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

		protected override void ProcessInputs()
		{
			outputPins[0].ReceiveInput(inputPins[0].State);
		}
	}
}
