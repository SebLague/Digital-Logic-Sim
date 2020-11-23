using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlUnit : CustomChip {

	const int clockInputPin = 4;
	const int instructionMSBPin = 0;
	const int carryFlagPin = 5;
	const int negativeFlagPin = 6;
	const int zeroFlagPin = 7;

	enum OutputPin {
		InstructionLoad,
		ValueOut,
		RAMLoad,
		RAMOut,
		MemAddressRegLoad,
		ProgramCounterJump,
		ProgramCounterIncrement,
		ProgramCounterOut,
		ARegisterLoad,
		ARegisterOut,
		BRegisterLoad,
		BRegisterOut,
		ALUSubtract,
		ALUOut,
		FlagLoad
	}

	int[] outputs;

	[Header ("Control Unit State")]
	public int counter;
	public int currentInstruction;
	int lastMessageStep = -1;
	ControlUnitDisplay display;
	int clockOld;
	bool isClockRisingEdge;
	bool isClockFallingEdge;

	protected override void Start () {
		base.Start ();
		display = FindObjectOfType<ControlUnitDisplay> ();
	}

	public override void ReceiveInputSignal (Pin pin) {
		base.ReceiveInputSignal (pin);
		//Debug.Log (pin.pinName);
		if (pin.parentPin) {
			//Debug.Log ("Input received from: " + pin.parentPin.pinName + " on pin: " + pin.pinName + "  " + pin.State);
		} else {
			//Debug.Log ("Const input on: " + pin.pinName + "  " + pin.State);
		}
	}

	protected override void ProcessOutput () {
		outputs = new int[outputPins.Length];

		isClockRisingEdge = false;
		// Increment counter output with each clock pulse
		int clock = inputPins[clockInputPin].currentState;
		isClockRisingEdge = (clock == 1 && clockOld == 0);
		isClockFallingEdge = (clock == 0 && clockOld == 1);
		clockOld = clock;

		if (isClockFallingEdge) {
			counter++;
		}

		display.controlUnitStepCounter = counter;
		//Debug.Log ("clock: " + clock + " count input: " + counterInput + " count: " + stepCounter + "  ready:" + hasSkippedFirstClock);
		// Process instruction
		int instruction = 0;
		for (int i = 0; i < 4; i++) {
			int pinState = inputPins[instructionMSBPin + i].currentState;
			instruction |= pinState << (3 - i);
		}
		currentInstruction = instruction;
		ProcessInstruction (instruction);

		SendOutputs ();

	}

	void SetOutput (OutputPin outputPin, bool active) {
		if (active) {
			outputs[(int) outputPin] = 1;
		}
	}

	void SetOutput (OutputPin outputPin, int signal) {
		outputs[(int) outputPin] = signal;
	}

	void ProcessInstruction (int instruction) {
		display.ActiveInstruction (instruction);

		// Fetch
		LogMessage ("Load address from program counter into memory address register", 0);
		SetOutput (OutputPin.ProgramCounterOut, counter == 0);
		SetOutput (OutputPin.MemAddressRegLoad, counter == 0);
		LogMessage ("Load instruction from RAM into instruction register and increment the program counter", 1);
		SetOutput (OutputPin.RAMOut, counter == 1);
		SetOutput (OutputPin.InstructionLoad, counter == 1);
		SetOutput (OutputPin.ProgramCounterIncrement, counter == 1);

		if (counter <= 1) {
			display.ActiveInstruction ("Fetch");
		} else {
			// Note 'X' denotes the 4bit value that comes along with the instruction (in the 4 least significant bits)
			switch (instruction) {
				case 0b0000: // A = RAM[X]
					LogInstruction ("A=RAM[X]");
					LogMessage ("Load value from value register into memory address register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.MemAddressRegLoad, counter == 2);
					LogMessage ("Load value from RAM into A register", 3);
					SetOutput (OutputPin.RAMOut, counter == 3);
					SetOutput (OutputPin.ARegisterLoad, counter == 3);
					ResetCounter (counter == 4);
					break;
				case 0b0001: // A = X
					LogInstruction ("A=X");
					LogMessage ("Load value from instruction value register into A register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.ARegisterLoad, counter == 2);
					ResetCounter (counter == 3);
					break;
				case 0b0010: // A = A + B
					LogInstruction ("A=A+B");
					LogMessage ("Load ALU output (A+B) into A Register. Also load ALU flags into FLAG register", 2);
					SetOutput (OutputPin.ARegisterOut, counter == 2);
					SetOutput (OutputPin.BRegisterOut, counter == 2);
					SetOutput (OutputPin.FlagLoad, counter == 2);
					SetOutput (OutputPin.ALUOut, counter == 2);
					SetOutput (OutputPin.ARegisterLoad, counter == 2);
					ResetCounter (counter == 3);
					break;
				case 0b0011: // A = A - B
					LogInstruction ("A=A-B");
					LogMessage ("Load ALU output (A-B) into A Register. Also load ALU flags into FLAG register", 2);
					SetOutput (OutputPin.ARegisterOut, counter == 2);
					SetOutput (OutputPin.BRegisterOut, counter == 2);
					SetOutput (OutputPin.ALUSubtract, counter == 2);
					SetOutput (OutputPin.ALUOut, counter == 2);
					SetOutput (OutputPin.FlagLoad, counter == 2);
					SetOutput (OutputPin.ARegisterLoad, counter == 2);
					ResetCounter (counter == 3);
					break;
				case 0b0100: // B = RAM[X]
					LogMessage ("Load value from value register into memory address register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.MemAddressRegLoad, counter == 2);
					LogMessage ("Load value from RAM into B register", 3);
					SetOutput (OutputPin.RAMOut, counter == 3);
					SetOutput (OutputPin.BRegisterLoad, counter == 3);
					ResetCounter (counter == 4);
					break;
				case 0b0101: // B = X
					LogInstruction ("B=X");
					LogMessage ("Load value from instruction value register into B register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.BRegisterLoad, counter == 2);
					ResetCounter (counter == 3);
					break;
				case 0b0110: // RAM[X] = A
					LogInstruction ("RAM[X]=A");
					LogMessage ("Load value from VALUE register into MEM ADDRESS register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.MemAddressRegLoad, counter == 2);
					LogMessage ("Load value from A register (through ALU) into RAM", 3);
					SetOutput (OutputPin.ARegisterOut, counter == 3);
					SetOutput (OutputPin.ALUOut, counter == 3);
					SetOutput (OutputPin.RAMLoad, counter == 3);
					ResetCounter (counter == 4);
					break;
				case 0b0111: // RAM[X] = B

					break;
				case 0b1000: // RAM[X] = A + B
					LogInstruction ("RAM[X]=A+B");
					LogMessage ("Load value from VALUE register into MEM ADDRESS register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.MemAddressRegLoad, counter == 2);
					LogMessage ("Load ALU output (A+B) into RAM. Also load ALU flags into FLAG register", 2);
					SetOutput (OutputPin.ARegisterOut, counter == 3);
					SetOutput (OutputPin.BRegisterOut, counter == 3);
					SetOutput (OutputPin.FlagLoad, counter == 3);
					SetOutput (OutputPin.ALUOut, counter == 3);
					SetOutput (OutputPin.RAMLoad, counter == 3);
					ResetCounter (counter == 4);
					break;
				case 0b1001: // RAM[X] = A - B
					LogInstruction ("RAM[X]=A-B");
					LogMessage ("Load value from VALUE register into MEM ADDRESS register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.MemAddressRegLoad, counter == 2);
					LogMessage ("Load ALU output (A-B) into RAM. Also load ALU flags into FLAG register", 2);
					SetOutput (OutputPin.ARegisterOut, counter == 3);
					SetOutput (OutputPin.BRegisterOut, counter == 3);
					SetOutput (OutputPin.FlagLoad, counter == 3);
					SetOutput (OutputPin.ALUSubtract, counter == 3);
					SetOutput (OutputPin.ALUOut, counter == 3);
					SetOutput (OutputPin.RAMLoad, counter == 3);
					ResetCounter (counter == 4);
					break;
				case 0b1010: // JMP[X]
					LogInstruction ("JMP[X]");
					LogMessage ("Jump program counter address to value in VALUE register", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.ProgramCounterJump, counter == 2);
					ResetCounter (counter == 3);
					break;
				case 0b1011: // JMP[X] IF NEG
					LogInstruction ("JMP[X] IF NEG");
					LogMessage ("Jump program counter address to value in VALUE register if NEG flag is on", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.ProgramCounterJump, (counter == 2) && (inputPins[negativeFlagPin].State == 1));
					ResetCounter (counter == 3);
					break;
				case 0b1100:
					LogInstruction ("Jump if zero");
					LogMessage ("Jump program counter address to value in value register if zero flag is on", 2);
					SetOutput (OutputPin.ValueOut, counter == 2);
					SetOutput (OutputPin.ProgramCounterJump, (counter == 2) && (inputPins[zeroFlagPin].State == 1));
					ResetCounter (counter == 3);
					break;
				case 0b1101:

					break;
				case 0b1110:

					break;
				case 0b1111:

					break;
				default:
					//ResetCounter (true);
					break;
			}
		}
	}

	void LogMessage (string message, int step) {
		if (counter == step && counter != lastMessageStep) {
			lastMessageStep = counter;
			display.Message (message);
		}
	}

	void LogInstruction (string message) {
		display.ActiveInstruction (message);
	}

	void ResetCounter (bool reset) {
		if (reset) {
			if (isClockFallingEdge) {
				Debug.Log ("Reset counter");
				counter = 0;
			}
		}
	}

	void SendOutputs () {
		for (int i = 0; i < outputs.Length; i++) {
			outputPins[i].ReceiveSignal (outputs[i]);
		}
	}

}