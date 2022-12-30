namespace DLS.Simulation.ChipImplementation
{
	using static PinState;

	public class BuiltinClock : BuiltinSimChip
	{
		public BuiltinClock(SimPin[] inputPins, SimPin[] outputPins) : base(inputPins, outputPins) { }

		protected override void ProcessInputs()
		{
			// Calculate frequency mode from inputs. Possible values: 0, 1, 2, 3
			int frequencyMode = inputPins[0].State.ToInt() << 1 | inputPins[1].State.ToInt();
			PinState outputState = ClockIsHigh(frequencyMode) ? HIGH : LOW;
			outputPins[0].ReceiveInput(outputState);

			bool ClockIsHigh(int frequencyMode)
			{
				switch (frequencyMode)
				{
					case 0: return Simulator.Time % 1 >= 0.5f; // One cycle per second
					case 1: return Simulator.Time % 0.5f >= 0.25f; // One cycle per half second
					case 2: return Simulator.FrameCount % 16 >= 8; // One cycle per 16 frames
					case 3: return Simulator.FrameCount % 4 >= 2; // One cycle per 4 frames
					default: return false;
				}
			}
		}
	}
}
