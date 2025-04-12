using System;
using DLS.Description;

namespace DLS.Simulation
{
	public class SimPin
	{
		public readonly int ID;
		public readonly SimChip parentChip;
		public readonly PinState State;

		public SimPin[] ConnectedTargetPins = Array.Empty<SimPin>();

		// Simulation frame index on which pin last received an input
		public int lastUpdatedFrameIndex;

		public int latestSourceID;
		public int latestSourceParentChipID;

		// Number of wires that input their signal to this pin.
		// (In the case of conflicting signals, the pin chooses randomly)
		public int numInputConnections;
		public int numInputsReceivedThisFrame;

		public SimPin(int id, PinBitCount bitCount, bool isInput, SimChip parentChip)
		{
			this.parentChip = parentChip;
			ID = id;

			State = new PinState((int)bitCount);
			if (!isInput) State.SetAllDisconnected();
		}

		public bool FirstBitHigh => State.GetBit(0) == PinState.LogicHigh;

		public void PropagateSignal()
		{
			int length = ConnectedTargetPins.Length;
			for (int i = 0; i < length; i++)
			{
				ConnectedTargetPins[i].ReceiveInput(this);
			}
		}

		void ReceiveInput(SimPin source)
		{
			// If this is the first input of the frame, reset the received inputs counter to zero
			if (lastUpdatedFrameIndex != Simulator.simulationFrame)
			{
				lastUpdatedFrameIndex = Simulator.simulationFrame;
				numInputsReceivedThisFrame = 0;
			}

			bool set = false;

			if (numInputsReceivedThisFrame > 0)
			{
				// Has already received input this frame, so choose at random whether to accept conflicting input.
				// Note: for multi-bit pins, this choice is made identically for all bits, rather than individually. (This is only
				// because it's easier to track the correct display colour this way, but maybe consider changing to per-bit in the future...)
				bool acceptConflictingInput = Simulator.RandomBool();

				for (int i = 0; i < State.BitCount; i++)
				{
					uint targetLogicLevel = source.State.GetBit(i);
					uint currentLogicLevel = State.GetBit(i);

					// Ignore disconnected input. Always accept input if current state is disconnected. Otherwise, choose randomly.
					if (targetLogicLevel != PinState.LogicDisconnected && (currentLogicLevel == PinState.LogicDisconnected || acceptConflictingInput))
					{
						State.SetBit(i, targetLogicLevel);
						set = true;
					}
				}
			}
			else
			{
				// First input source this frame, so accept it.
				State.SetFromSource(source.State);
				set = true;
			}

			if (set)
			{
				latestSourceID = source.ID;
				latestSourceParentChipID = source.parentChip.ID;
			}

			numInputsReceivedThisFrame++;

			if (numInputsReceivedThisFrame == numInputConnections)
			{
				if (Simulator.simulationFrame > parentChip.lastReceivedInputFrame)
				{
					parentChip.numInputsReady = 0;
					parentChip.lastReceivedInputFrame = Simulator.simulationFrame;
				}

				parentChip.numInputsReady++;
			}
		}
	}
}