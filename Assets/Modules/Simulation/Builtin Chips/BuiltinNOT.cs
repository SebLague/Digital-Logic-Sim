namespace DLS.Simulation.ChipImplementation
{
	using static PinState;
	public class BuiltinNOT : BuiltinSimChip
	{
		public BuiltinNOT(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

		protected override void ProcessInputs()
		{
			outputPins[0].ReceiveInput(inputPins[0].State == HIGH ? LOW : HIGH);
		}
	}
}
