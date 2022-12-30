namespace DLS.Simulation
{
	// Builtin chips handle the core logic operations that the simulation is built on, such as AND and NOT.
	// Other higher-level chips may be implemented here for efficiency and convience if needed (such as a builtin RAM chip, and so on).
	public abstract class BuiltinSimChip
	{

		protected readonly SimPin[] inputPins;
		protected readonly SimPin[] outputPins;
		protected readonly int numInputs;
		int numUnprocessedInputsReceived;


		public BuiltinSimChip(SimPin[] inputPins, SimPin[] outputPins)
		{
			this.inputPins = inputPins;
			this.outputPins = outputPins;
			numInputs = inputPins.Length;
		}

		public void SetInputPin(int index, SimPin pin)
		{
			inputPins[index] = pin;
		}

		public void SetOutputPin(int index, SimPin pin) => outputPins[index] = pin;

		public void InputReceived()
		{
			numUnprocessedInputsReceived++;
			//UnityEngine.Debug.Log(GetType().ToString() + "  Received input: " + numUnprocessedInputsReceived + " / " + numConnectedInputs);
			if (numUnprocessedInputsReceived == numInputs)
			{
				ProcessInputs();
				numUnprocessedInputsReceived = 0;
			}
		}

		protected abstract void ProcessInputs();

		void ProcessAND()
		{
			bool result = inputPins[0].State == PinState.HIGH && inputPins[1].State == PinState.HIGH;
			outputPins[0].ReceiveInput(result ? PinState.HIGH : PinState.LOW);
		}

		void ProcessNOT()
		{
			outputPins[0].ReceiveInput(inputPins[0].State == PinState.HIGH ? PinState.LOW : PinState.HIGH);
		}

		void ProcessTriStateBuffer()
		{
			PinState enable = inputPins[0].State;
			PinState data = inputPins[1].State;
			PinState output = enable is PinState.HIGH ? data : PinState.FLOATING;
			outputPins[0].ReceiveInput(output);
		}

		void ProcessBus()
		{
			outputPins[0].ReceiveInput(inputPins[0].State);
		}

		void ProcessClock()
		{
			// Calculate frequency mode from inputs. Possible values: 0, 1, 2, 3
			int frequencyMode = inputPins[0].State.ToInt() << 1 | inputPins[1].State.ToInt();
			PinState outputState = ClockIsHigh(frequencyMode) ? PinState.HIGH : PinState.LOW;
			outputPins[0].ReceiveInput(outputState);

			bool ClockIsHigh(int frequencyMode)
			{
				switch (frequencyMode)
				{
					case 0: return Simulator.Time % 1 > 0.5f; // One cycle per second
					case 1: return Simulator.Time % 0.5f > 0.25f; // One cycle per half second
					case 2: return Simulator.FrameCount % 16 >= 7; // One cycle per 16 frames
					case 3: return Simulator.FrameCount % 8 >= 3; // One cycle per 8 frames
					default: return false;
				}
			}
		}
	}

}