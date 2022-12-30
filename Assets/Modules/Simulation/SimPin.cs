using System.Collections;
using System.Collections.Generic;


namespace DLS.Simulation
{
	// Represents a pin inside the simulation.
	// A pin receives an input signal (Low, High, or High-Z) from another pin (typically the output pin of some chip).
	// If this pin is the input pin of a built-in chip, then the built-in chip is notified so that it can process the signal.
	// Otherwise, it simply propagates the signal on to any pins that it is connected to.

	// Note: pins can receive multiple inputs. In this case the pin will wait until all signals have been received before acting.
	// In correct usage, only one of the signals should be High/Low, and the rest should be High-Z. If conflicting High/Low inputs are
	// received, then a random one will be chosen on each step of the simulation.
	public class SimPin
	{
		public PinState State;
		PinState nextState;

		public List<SimPin> connectedPins;

		public readonly bool isBuiltinChipInputPin;
		public readonly bool isInputPin;
		BuiltinSimChip builtinChip;

		public bool isFloating;
		public bool cycleFlag;

		public int numInputs;
		int numInputsReceivedSinceSignalPropagated;

		public string debugName;
		public readonly int ID;
		static System.Random rng;

		public SimPin(BuiltinSimChip builtinChipToGiveInputTo, bool isInputPin, string name, int id)
		{
			this.debugName = name;
			this.isInputPin = isInputPin;
			isFloating = true;
			this.ID = id;

			if (builtinChipToGiveInputTo is null)
			{
				connectedPins = new List<SimPin>();
			}
			else
			{
				isBuiltinChipInputPin = true;
				builtinChip = builtinChipToGiveInputTo;
			}
		}


		// Receive input (either from another pin, from a built-in chip, or directly from the simulator if this is a floating pin)
		public void ReceiveInput(PinState inputState)
		{
			// Update the state of this pin based on the incoming state.
			// Note: if this pin receives inputs from multiple sources, all inputs should be tri-stated except for one.
			// If a conflicting HIGH/LOW signal is received, a random one will be selected.
			if (nextState == PinState.FLOATING)
			{
				nextState = inputState;
			}
			else if (inputState != PinState.FLOATING)
			{
				rng ??= new System.Random();
				nextState = rng.NextDouble() < 0.5 ? nextState : inputState;
			}

			numInputsReceivedSinceSignalPropagated++;
			//Debug.Log("Set State: " + debugName + "  " + state.ToString());

			if (!cycleFlag && numInputsReceivedSinceSignalPropagated >= numInputs)
			{
				PropagateSignal();
			}

		}

		// Send the signal received by this pin forwards to any pins (or to the built-in chip) that it's connected to.
		public void PropagateSignal()
		{
			State = nextState;
			nextState = PinState.FLOATING;

			// If this is the input pin of a builtin chip, then notify the chip that the pin's state has been resolved and is ready to be used
			if (isBuiltinChipInputPin)
			{
				builtinChip.InputReceived();
			}
			// Otherwise, propagate the signal forward to any connected pins
			else
			{
				for (int i = 0; i < connectedPins.Count; i++)
				{
					connectedPins[i].ReceiveInput(State);
				}
			}
			numInputsReceivedSinceSignalPropagated = 0;
		}

		public void MarkCyclic()
		{
			cycleFlag = true;
		}

		// Add a target pin to which this pin should forward any signals it receives
		public void AddConnectedPin(SimPin target)
		{
			connectedPins.Add(target);
			target.OnIncomingConnectionCreated(this);
		}

		// Remove connection to the target pin so that the target pin no longer receives signals from this pin.
		public void RemoveConnectedPin(SimPin target)
		{
			connectedPins.Remove(target);
			target.OnIncomingConnectionRemoved(this);
		}

		// Notification that this pin will now be receiving signals from the source pin
		void OnIncomingConnectionCreated(SimPin source)
		{
			numInputs++;
			isFloating = false;
		}

		// Notification that this pin will no longer be receiving signals from the source pin
		void OnIncomingConnectionRemoved(SimPin source)
		{
			numInputs--;
			isFloating = numInputs == 0;
		}

	}
}